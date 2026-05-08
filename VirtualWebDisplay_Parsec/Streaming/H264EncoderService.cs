using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;
using static Sdcb.FFmpeg.Raw.ffmpeg;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.Streaming;

/// <summary>
/// Encodes raw BGRA32 frames (from <see cref="IFrameSource.RawFrameAvailable"/>) to
/// H.264 NAL units using the best available encoder:
/// NVIDIA NVENC → AMD AMF → Intel QSV → libx264 (CPU fallback).
///
/// Encoded NAL units are raised via <see cref="NalUnitReady"/>, consumed by
/// <see cref="WebRtcStreamService"/> to feed the WebRTC VideoTrack.
/// </summary>
internal sealed class H264EncoderService : BackgroundService
{
    private readonly IFrameSource _frameSource;
    private readonly VirtualScreenConfig _config;
    private readonly ILogger<H264EncoderService> _logger;

    // Temporary diagnostics for encoder throughput.
#if DEBUG
    private const double EncoderStatsLogIntervalSeconds = 5.0;
    private long _statsWindowStartTicks = Stopwatch.GetTimestamp();
    private long _statsEncodedPackets;
    private long _statsEncodedBytes;
    private int _loggedPacketFormat;
#endif

    // Bounded channel: if the encoder falls behind, drop the oldest frame rather than
    // accumulating unbounded memory. Capacity 3 = ~100 ms at 30 fps.
    private readonly Channel<RawFrame> _channel = Channel.CreateBounded<RawFrame>(
        new BoundedChannelOptions(3)
        {
            FullMode       = BoundedChannelFullMode.DropOldest,
            SingleReader   = true,
            SingleWriter   = false
        });

    /// <summary>
    /// Fired on the encoder thread for every encoded NAL unit.
    /// Payload is (nalBytes, sourceTimestampUs).
    /// </summary>
    public event Action<byte[], long>? NalUnitReady;

    internal H264EncoderService(
        IFrameSource frameSource,
        VirtualScreenConfig config,
        ILogger<H264EncoderService> logger)
    {
        _frameSource = frameSource;
        _config      = config;
        _logger      = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _frameSource.RawFrameAvailable += OnRawFrameAvailable;
        try
        {
            await EncodeLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown, no-op.
        }
        catch (Exception ex)
        {
#if DEBUG
            _logger.LogError(ex, "H.264 encoder loop failed.");
    #else
            _ = ex;
#endif
        }
        finally
        {
            _frameSource.RawFrameAvailable -= OnRawFrameAvailable;
        }
    }

    private void OnRawFrameAvailable(RawFrame frame)
    {
        // Non-blocking write: DropOldest handles back-pressure.
        _channel.Writer.TryWrite(frame);
    }

    private async Task EncodeLoopAsync(CancellationToken ct)
    {
        var codec = FindBestH264Encoder(out string encoderName);
    #if DEBUG
        _logger.LogInformation("H.264 encoder selected: {Name}", encoderName);
    #endif

        int  framerate   = _config.H264Framerate   > 0 ? _config.H264Framerate   : 30;
        long bitrateKbps = _config.H264BitrateKbps > 0 ? _config.H264BitrateKbps : 2000;

        RawFrame? pendingFirstFrame = null;

        while (!ct.IsCancellationRequested)
        {
            // Bind encoder dimensions to the first frame of each session.
            // If capture resolution changes at runtime, we restart the session.
            var firstFrame = pendingFirstFrame ?? await ReadFirstFrameAsync(ct);
            pendingFirstFrame = null;

            int width = firstFrame.Width;
            int height = firstFrame.Height;
#if DEBUG
            _logger.LogInformation("H.264 encoder frame size: {Width}x{Height}", width, height);
#endif

            // Each encoder family requires a specific input pixel format.
            // QSV only accepts nv12/qsv; everything else works with yuv420p.
            var encoderPixFmt = GetEncoderPixelFormat(encoderName);

            using var enc = new CodecContext(codec);
            enc.Width       = width;
            enc.Height      = height;
            enc.PixelFormat = encoderPixFmt;
            enc.BitRate     = bitrateKbps * 1000L;
            enc.TimeBase    = new AVRational { Num = 1,         Den = framerate };
            enc.Framerate   = new AVRational { Num = framerate, Den = 1 };
            enc.GopSize     = framerate * 2;  // keyframe every ~2 s
            enc.MaxBFrames  = 0;              // no B-frames → lower latency

            using var opts = new MediaDictionary();
            ApplyEncoderPreset(encoderName, opts);
            enc.Open(codec, opts);

            using var converter = new VideoFrameConverter();
            using var bgraFrame = Frame.CreateVideo(width, height, AVPixelFormat.Bgra);
            using var yuvFrame  = Frame.CreateVideo(width, height, encoderPixFmt);
            using var packet    = new Packet();

            long frameIndex = 0;
            long minFrameIntervalUs = Math.Max(1, 1_000_000L / framerate);
            long lastEncodedTimestampUs = firstFrame.TimestampUs;

            EncodeFrame(firstFrame, converter, bgraFrame, yuvFrame, enc, packet, ref frameIndex);

            bool restartForResolutionChange = false;

            while (!ct.IsCancellationRequested && await _channel.Reader.WaitToReadAsync(ct))
            {
                while (_channel.Reader.TryRead(out var rawFrame))
                {
                    if (rawFrame.Width != width || rawFrame.Height != height)
                    {
#if DEBUG
                        _logger.LogInformation(
                            "H.264 encoder resolution changed from {OldW}x{OldH} to {NewW}x{NewH}; restarting encoder session.",
                            width, height, rawFrame.Width, rawFrame.Height);
#endif
                        pendingFirstFrame = rawFrame;
                        restartForResolutionChange = true;
                        break;
                    }

                    if (rawFrame.TimestampUs - lastEncodedTimestampUs < minFrameIntervalUs)
                        continue;

                    lastEncodedTimestampUs = rawFrame.TimestampUs;
                    EncodeFrame(rawFrame, converter, bgraFrame, yuvFrame, enc, packet, ref frameIndex);
                }

                if (restartForResolutionChange)
                    break;
            }

            if (!restartForResolutionChange && pendingFirstFrame is null)
                break;
        }
    }

    private async Task<RawFrame> ReadFirstFrameAsync(CancellationToken ct)
    {
        while (await _channel.Reader.WaitToReadAsync(ct))
        {
            if (_channel.Reader.TryRead(out var rawFrame))
                return rawFrame;
        }

        throw new OperationCanceledException(ct);
    }

    private void EncodeFrame(
        RawFrame rawFrame,
        VideoFrameConverter converter,
        Frame bgraFrame,
        Frame yuvFrame,
        CodecContext enc,
        Packet packet,
        ref long frameIndex)
    {
        // 1. Copy BGRA source bytes to the AVFrame honoring destination stride.
        CopyBgraToFrame(rawFrame.Data, rawFrame.Width, rawFrame.Height, bgraFrame);

        // 2. Convert BGRA → YUV420p.
        converter.ConvertFrame(bgraFrame, yuvFrame);

        // 3. Stamp PTS and encode. Pass unref:false to keep our frame buffers alive.
        yuvFrame.Pts = frameIndex++;
        foreach (var pkt in enc.EncodeFrame(yuvFrame, packet, unref: false))
        {
            EmitPacket(pkt, rawFrame.TimestampUs);
            pkt.Unref();
        }
    }

    private static unsafe void CopyBgraToFrame(byte[] sourceBgra, int width, int height, Frame destination)
    {
        int rowBytes = width * 4;
        AVFrame* dst = destination;
        byte* dstData = (byte*)dst->data[0];
        int dstStride = dst->linesize[0];

        if (dstData is null || dstStride <= 0)
            throw new InvalidOperationException("Invalid destination frame buffer for BGRA upload.");

        fixed (byte* srcBase = sourceBgra)
        {
            for (int y = 0; y < height; y++)
            {
                byte* srcRow = srcBase + (long)y * rowBytes;
                byte* dstRow = dstData + (long)y * dstStride;
                Buffer.MemoryCopy(srcRow, dstRow, rowBytes, rowBytes);
            }
        }
    }

    private unsafe void EmitPacket(Packet pkt, long timestampUs)
    {
        AVPacket* raw = pkt;
        if (raw->size <= 0) return;

        var packetBytes = new byte[raw->size];
        Marshal.Copy((nint)raw->data, packetBytes, 0, raw->size);

        var bytes = NormalizeToAnnexB(packetBytes);
        if (bytes.Length == 0)
            return;

#if DEBUG
        Interlocked.Increment(ref _statsEncodedPackets);
        Interlocked.Add(ref _statsEncodedBytes, bytes.Length);
        LogEncoderStatsIfNeeded();
#endif

        NalUnitReady?.Invoke(bytes, timestampUs);
    }

    private byte[] NormalizeToAnnexB(byte[] packet)
    {
        if (packet.Length < 4)
            return packet;

        if (HasAnnexBStartCode(packet))
        {
            LogPacketFormatOnce("annexb (start code)");
            return packet;
        }

        if (!LooksLikeAvccPacket(packet))
        {
            LogPacketFormatOnce("unknown/raw (no Annex-B start code, no AVCC parse)");
            return packet;
        }

        try
        {
            using var ms = new MemoryStream(packet.Length + 64);
            int offset = 0;

            while (offset + 4 <= packet.Length)
            {
                int nalLength = (packet[offset] << 24)
                              | (packet[offset + 1] << 16)
                              | (packet[offset + 2] << 8)
                              | packet[offset + 3];
                offset += 4;

                if (nalLength <= 0 || offset + nalLength > packet.Length)
                {
                    LogPacketFormatOnce("avcc parse fallback to original packet");
                    return packet;
                }

                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(1);
                ms.Write(packet, offset, nalLength);
                offset += nalLength;
            }

            if (offset != packet.Length || ms.Length == 0)
            {
                LogPacketFormatOnce("avcc partial parse fallback to original packet");
                return packet;
            }

            LogPacketFormatOnce("avcc -> annexb conversion");
            return ms.ToArray();
        }
        catch
        {
            return packet;
        }
    }

    private static bool HasAnnexBStartCode(byte[] bytes)
    {
        if (bytes.Length < 4)
            return false;

        if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1)
            return true;

        if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 1)
            return true;

        return false;
    }

    private static bool LooksLikeAvccPacket(byte[] bytes)
    {
        if (bytes.Length < 8)
            return false;

        int firstNalLength = (bytes[0] << 24)
                           | (bytes[1] << 16)
                           | (bytes[2] << 8)
                           | bytes[3];

        return firstNalLength > 0 && firstNalLength <= bytes.Length - 4;
    }

    private void LogPacketFormatOnce(string format)
    {
#if DEBUG
        if (Interlocked.Exchange(ref _loggedPacketFormat, 1) == 0)
            _logger.LogInformation("H.264 packet format detected: {Format}.", format);
#endif
    }

    private void LogEncoderStatsIfNeeded()
    {
#if DEBUG
        var nowTicks = Stopwatch.GetTimestamp();
        var startTicks = Volatile.Read(ref _statsWindowStartTicks);
        double elapsedSeconds = (nowTicks - startTicks) / (double)Stopwatch.Frequency;
        if (elapsedSeconds < EncoderStatsLogIntervalSeconds)
            return;

        if (Interlocked.CompareExchange(ref _statsWindowStartTicks, nowTicks, startTicks) != startTicks)
            return;

        long packets = Interlocked.Exchange(ref _statsEncodedPackets, 0);
        long bytes = Interlocked.Exchange(ref _statsEncodedBytes, 0);
        long avgPacket = packets > 0 ? bytes / packets : 0;
        double kbps = elapsedSeconds > 0 ? (bytes * 8.0 / 1000.0) / elapsedSeconds : 0;

        _logger.LogInformation(
            "H.264 encoder stats ({Seconds:F1}s): packets={Packets}, avgPacketBytes={AvgPacket}, bitrateKbps={Kbps:F1}.",
            elapsedSeconds,
            packets,
            avgPacket,
            kbps);
        #endif
    }

    private void ApplyEncoderPreset(string encoderName, MediaDictionary opts)
    {
        switch (encoderName)
        {
            case "h264_nvenc":
                opts["preset"] = "p1";    // fastest NVENC preset
                opts["tune"]   = "ull";   // ultra-low latency
                opts["rc"]     = "cbr";
                opts["repeat_headers"] = "1";
                break;
            case "h264_amf":
                opts["usage"]  = "ultralowlatency";
                opts["profile"]= "baseline";
                break;
            case "h264_qsv":
                opts["preset"] = "veryfast";
                opts["profile"]= "baseline";
                break;
            default: // libx264
                opts["preset"] = "ultrafast";
                opts["tune"]   = "zerolatency";
                opts["x264-params"] = "repeat-headers=1:aud=1";
                break;
        }
    }

    private Codec FindBestH264Encoder(out string name)
    {
        // h264_qsv is excluded: on systems where the Intel MFX runtime DLL is present but
        // QSV initialisation fails, enc.Open() triggers a native SEH crash (0xC0000005)
        // that cannot be caught by C# — it kills the process before GDI capture starts.
        // libx264 (bundled via Sdcb.FFmpeg.runtime) is always safe and sufficient.
        string[] candidates = ["h264_nvenc", "h264_amf", "libx264"];
        foreach (var candidate in candidates)
        {
            // Pre-check: verify the hardware runtime DLL is loadable before letting
            // FFmpeg attempt to initialize the encoder. NVENC/AMF/QSV will crash with
            // a native AV (0xC0000005) if their SDK DLL is missing — not catchable.
            if (!HasRequiredRuntime(candidate))
            {
#if DEBUG
                _logger.LogDebug("H.264 encoder {Name} skipped (runtime DLL not available).", candidate);
#endif
                continue;
            }

            try
            {
                Codec codec = Codec.FindEncoderByName(candidate).GetValueOrDefault();
                name = candidate;
                return codec;
            }
            catch
            {
#if DEBUG
                _logger.LogDebug("H.264 encoder {Name} not available.", candidate);
#endif
            }
        }
        throw new InvalidOperationException(
            "No H.264 encoder found. Ensure libx264 or a GPU encoder (nvenc/amf/qsv) is available.");
    }

    /// <summary>
    /// Returns the input pixel format that the given encoder accepts.
    /// QSV only supports nv12/qsv; everything else works with yuv420p.
    /// Using the wrong format causes enc.Open() to fail, which corrupts
    /// the internal AVDictionary pointer and crashes on disposal (0xC0000005).
    /// </summary>
    private static AVPixelFormat GetEncoderPixelFormat(string encoderName) => encoderName switch
    {
        "h264_qsv" => AVPixelFormat.Nv12,
        _          => AVPixelFormat.Yuv420p,
    };

    /// <summary>
    /// Returns true if the native SDK DLL required by the encoder can be loaded.
    /// Using <see cref="NativeLibrary.TryLoad"/> is safe — it returns false without
    /// crashing if the library is absent.
    /// </summary>
    private static bool HasRequiredRuntime(string encoderName) => encoderName switch
    {
        "h264_nvenc" => TryLoadAndFree("nvcuda.dll"),
        "h264_amf"   => TryLoadAndFree("amfrt64.dll"),
        _            => true,  // libx264 is bundled with Sdcb.FFmpeg.runtime
    };

    private static bool TryLoadAndFree(string libraryName)
    {
        if (!NativeLibrary.TryLoad(libraryName, out var handle))
            return false;

        NativeLibrary.Free(handle);
        return true;
    }
}
