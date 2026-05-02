using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Interop;

namespace VirtualWebDisplay.Streaming;

public sealed class CaptureService : BackgroundService
{

    private readonly VirtualScreenConfig _config;
    private readonly ILogger<CaptureService> _logger;
    private byte[] _currentFrame = [];
    private readonly Lock _frameLock = new();
    private ulong _lastRawHash;

    // Codec cached once to avoid repeated lookups
    private static readonly ImageCodecInfo JpegCodec =
        ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

    public CaptureService(VirtualScreenConfig config, ILogger<CaptureService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>Returns the latest captured frame as a JPEG byte array. Empty if not yet captured.</summary>
    public byte[] GetCurrentFrame()
    {
        lock (_frameLock) return _currentFrame;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var captureStart = now;
            var interval = TimeSpan.FromSeconds(TransmissionModeOptions.GetEffectiveCaptureIntervalSeconds(_config));

            try
            {
                using var bitmap = CaptureFrame();
                if (bitmap is null)
                {
                    await Task.Delay(500, stoppingToken); // Wait a bit before retrying on capture failure
                    continue;
                }

                var rawHash = SampleHash(bitmap);
                if (rawHash == _lastRawHash)
                {
                    // Screen unchanged â€” skip encoding entirely.
                    var delay2 = interval - (DateTime.UtcNow - captureStart);
                    if (delay2 > TimeSpan.Zero)
                        await Task.Delay(delay2, stoppingToken);
                    continue;
                }
                _lastRawHash = rawHash;

                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)TransmissionModeOptions.GetEffectiveJpegQuality(_config));

                using var ms = new MemoryStream();
                bitmap.Save(ms, JpegCodec, encoderParams);

                lock (_frameLock)
                    _currentFrame = ms.ToArray();
            }
            catch (Exception ex)
            {
                // Capture errors are transient (e.g. screen lock, minimized); keep running.
                _logger.LogWarning(ex, "Transient capture error on monitor {MonitorIndex}.", _config.MonitorIndex);
            }

            var delay = interval - (DateTime.UtcNow - captureStart);
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);
        }
    }

    private Bitmap? CaptureFrame()
    {
        var region = GetCaptureRegion();
        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
            DrawCursorIfVisible(g, region);
        }

        return bitmap;
    }

    private static void DrawCursorIfVisible(Graphics g, Rectangle region)
    {
        var cursorInfo = new CursorNativeMethods.CURSORINFO { cbSize = Marshal.SizeOf<CursorNativeMethods.CURSORINFO>() };
        if (!CursorNativeMethods.GetCursorInfo(out cursorInfo)
            || (cursorInfo.flags & CursorNativeMethods.CursorShowing) == 0
            || cursorInfo.hCursor == IntPtr.Zero)
            return;

        if (!region.Contains(cursorInfo.ptScreenPos.X, cursorInfo.ptScreenPos.Y))
            return;

        var iconHandle = CursorNativeMethods.CopyIcon(cursorInfo.hCursor);
        if (iconHandle == IntPtr.Zero)
            return;

        CursorNativeMethods.ICONINFO iconInfo = default;

        try
        {
            if (!CursorNativeMethods.GetIconInfo(iconHandle, out iconInfo))
                return;

            using var icon = Icon.FromHandle(iconHandle);
            var x = cursorInfo.ptScreenPos.X - region.X - iconInfo.xHotspot;
            var y = cursorInfo.ptScreenPos.Y - region.Y - iconInfo.yHotspot;
            g.DrawIcon(icon, x, y);
        }
        finally
        {
            if (iconInfo.hbmMask != IntPtr.Zero)
                CursorNativeMethods.DeleteObject(iconInfo.hbmMask);

            if (iconInfo.hbmColor != IntPtr.Zero)
                CursorNativeMethods.DeleteObject(iconInfo.hbmColor);

            CursorNativeMethods.DestroyIcon(iconHandle);
        }
    }

    /// <summary>
    /// FNV-1a hash over a ~1.5% pixel sample grid (every 8th pixel on each axis).
    /// Fast enough to run on every frame; sensitive enough to detect any visible change.
    /// </summary>
    private static unsafe ulong SampleHash(Bitmap bmp)
    {
        const int xStep = 8;
        const int yStep = 8;
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var hash = 14695981039346656037UL;
            var stride = data.Stride;
            var ptr = (byte*)data.Scan0;
            for (var y = 0; y < bmp.Height; y += yStep)
            {
                var row = ptr + y * stride;
                for (var x = 0; x < bmp.Width; x += xStep)
                {
                    var px = *(uint*)(row + x * 4);
                    hash ^= px;
                    hash *= 1099511628211UL;
                }
            }
            return hash;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    private Rectangle GetCaptureRegion()
    {
        if (_config.MonitorIndex >= 0)
        {
            var screens = Screen.AllScreens;
            if (_config.MonitorIndex < screens.Length)
                return screens[_config.MonitorIndex].Bounds;
        }

        return Screen.PrimaryScreen?.Bounds
            ?? new Rectangle(0, 0, _config.Width, _config.Height);
    }
}
