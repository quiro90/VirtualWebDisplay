﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Streaming;

/// <summary>
/// Manages WebRTC peer connections and streams encoded H.264 video to each peer
/// using a native VideoTrack (RTP). Replaces the previous DataChannel-JPEG approach.
///
/// Each browser peer receives the same NAL units produced by <see cref="H264EncoderService"/>.
/// The pipeline is: DxgiCaptureService → H264EncoderService → WebRtcStreamService → browser.
/// </summary>
public sealed class WebRtcStreamService : BackgroundService, IAsyncDisposable
{
    // H.264 clock rate is always 90 000 Hz per RFC 6184.
    private const int H264ClockRate = 90_000;

    // Dynamic payload type in range 96–127 for H.264 (common convention).
    private const int H264PayloadTypeId = 96;

    private static readonly RTCConfiguration PeerConfiguration = new();
    private static readonly VideoFormat H264Format =
        new(VideoCodecsEnum.H264, H264PayloadTypeId, H264ClockRate,
            "level-asymmetry-allowed=1;packetization-mode=1");

    private readonly H264EncoderService _encoder;
    private readonly ILogger<WebRtcStreamService> _logger;
    private readonly ConcurrentDictionary<Guid, PeerState> _peers = new();

    // Temporary diagnostics (periodic logs to avoid per-frame noise).
#if DEBUG
    private const double StatsLogIntervalSeconds = 5.0;
    private long _statsWindowStartTicks = Stopwatch.GetTimestamp();
    private long _statsNalCount;
    private long _statsNalBytes;
    private long _statsSendOps;
    private long _statsSendFailures;
#endif

    internal WebRtcStreamService(H264EncoderService encoder, ILogger<WebRtcStreamService> logger)
    {
        _encoder = encoder;
        _logger  = logger;
    }

    public int ActivePeerCount => _peers.Count;

    /// <summary>
    /// Processes a WebRTC offer from the browser and returns the SDP answer.
    /// A local H.264 video track is added before the answer is generated so that
    /// the SDP negotiation includes the video m-line.
    /// </summary>
    public async Task<WebRtcSessionAnswer> CreateAnswerAsync(WebRtcSessionOffer offer, CancellationToken cancellationToken)
    {
        var peerId           = Guid.NewGuid();
        var peerConnection   = new RTCPeerConnection(PeerConfiguration);
        var iceGatheringDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

#if DEBUG
        _logger.LogInformation(
            "WebRTC offer received for peer {PeerId}. SDP length={SdpLength}, active peers={PeerCount}.",
            peerId,
            offer.Sdp?.Length ?? 0,
            _peers.Count);
#endif

        // Add a send-only H.264 video track before the answer is created.
        var videoTrack = new MediaStreamTrack(H264Format, MediaStreamStatusEnum.SendOnly);
        peerConnection.addTrack(videoTrack);

        peerConnection.onicecandidate += candidate =>
        {
            if (candidate is null || string.IsNullOrWhiteSpace(candidate.candidate))
                iceGatheringDone.TrySetResult(true);
        };

        peerConnection.onconnectionstatechange += state =>
        {
#if DEBUG
            _logger.LogDebug("Peer {PeerId} connection state: {State}.", peerId, state);
#endif
            if (state is RTCPeerConnectionState.closed or RTCPeerConnectionState.failed or RTCPeerConnectionState.disconnected)
                RemovePeer(peerId);
        };

        peerConnection.oniceconnectionstatechange += state =>
        {
#if DEBUG
            _logger.LogDebug("Peer {PeerId} ICE state: {State}.", peerId, state);
#endif
            if (state is RTCIceConnectionState.closed or RTCIceConnectionState.failed or RTCIceConnectionState.disconnected)
                RemovePeer(peerId);
        };

        var setResult = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
        {
            type = RTCSdpType.offer,
            sdp  = offer.Sdp,
        });

        if (!string.Equals(setResult.ToString(), "OK", StringComparison.OrdinalIgnoreCase))
        {
#if DEBUG
            _logger.LogWarning("Failed to apply WebRTC offer for peer {PeerId}: {Result}.", peerId, setResult);
#endif
            peerConnection.close();
            throw new InvalidOperationException($"Failed to apply the WebRTC offer ({setResult}).");
        }

        var answer = peerConnection.createAnswer(null);
        await peerConnection.setLocalDescription(answer);

        try
        {
            await iceGatheringDone.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
        }
        catch (TimeoutException)
        {
            // Continue with whatever candidates were gathered.
        }

        string localSdp = peerConnection.localDescription?.sdp?.ToString() ?? answer.sdp ?? string.Empty;
        int negotiatedPayloadType = ResolveH264PayloadType(localSdp, H264PayloadTypeId);
        _peers[peerId] = new PeerState(peerConnection, negotiatedPayloadType);

#if DEBUG
        _logger.LogInformation(
            "WebRTC answer created for peer {PeerId}. Local SDP length={SdpLength}, H264 payloadType={PayloadType}, active peers={PeerCount}.",
            peerId,
            localSdp.Length,
            negotiatedPayloadType,
            _peers.Count);
#endif
        return new WebRtcSessionAnswer(localSdp, "answer", peerId.ToString("N"));
    }

    private static int ResolveH264PayloadType(string sdp, int fallbackPayloadType)
    {
        if (string.IsNullOrWhiteSpace(sdp))
            return fallbackPayloadType;

        // Prefer explicit H264 rtpmap in the negotiated local SDP.
        var h264Map = Regex.Match(sdp, @"a=rtpmap:(\d+)\s+H264/90000", RegexOptions.IgnoreCase);
        if (h264Map.Success && int.TryParse(h264Map.Groups[1].Value, out var payloadType))
            return payloadType;

        return fallbackPayloadType;
    }

    /// <summary>
    /// Subscribes to <see cref="H264EncoderService.NalUnitReady"/> and forwards each
    /// NAL unit to all connected peers via RTP.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _encoder.NalUnitReady += OnNalUnitReady;
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            _encoder.NalUnitReady -= OnNalUnitReady;
        }
    }

    private void OnNalUnitReady(byte[] nalBytes, long timestampUs)
    {
        if (_peers.IsEmpty) return;

#if DEBUG
        Interlocked.Increment(ref _statsNalCount);
        Interlocked.Add(ref _statsNalBytes, nalBytes.Length);
#endif

        foreach (var entry in _peers)
        {
            bool sent = entry.Value.TrySendNal(nalBytes, timestampUs);
#if DEBUG
            if (sent)
                Interlocked.Increment(ref _statsSendOps);
            else
                Interlocked.Increment(ref _statsSendFailures);
#endif
        }

#if DEBUG
        LogForwardingStatsIfNeeded();
#endif
    }

    private void LogForwardingStatsIfNeeded()
    {
#if DEBUG
        var nowTicks = Stopwatch.GetTimestamp();
        var startTicks = Volatile.Read(ref _statsWindowStartTicks);
        double elapsedSeconds = (nowTicks - startTicks) / (double)Stopwatch.Frequency;
        if (elapsedSeconds < StatsLogIntervalSeconds)
            return;

        if (Interlocked.CompareExchange(ref _statsWindowStartTicks, nowTicks, startTicks) != startTicks)
            return;

        long nalCount = Interlocked.Exchange(ref _statsNalCount, 0);
        long nalBytes = Interlocked.Exchange(ref _statsNalBytes, 0);
        long sendOps = Interlocked.Exchange(ref _statsSendOps, 0);
        long sendFailures = Interlocked.Exchange(ref _statsSendFailures, 0);

        double kbps = elapsedSeconds > 0 ? (nalBytes * 8.0 / 1000.0) / elapsedSeconds : 0;
        long avgNal = nalCount > 0 ? nalBytes / nalCount : 0;

        _logger.LogInformation(
            "WebRTC forwarding stats ({Seconds:F1}s): peers={Peers}, nals={Nals}, avgNalBytes={AvgNal}, bitrateKbps={Kbps:F1}, sendOps={SendOps}, sendFailures={SendFailures}.",
            elapsedSeconds,
            _peers.Count,
            nalCount,
            avgNal,
            kbps,
            sendOps,
            sendFailures);
        #endif
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _encoder.NalUnitReady -= OnNalUnitReady;
        CloseAllPeers();
        await base.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _encoder.NalUnitReady -= OnNalUnitReady;
        CloseAllPeers();
        await base.StopAsync(CancellationToken.None);
        base.Dispose();
    }

    private void CloseAllPeers()
    {
        foreach (var peerId in _peers.Keys.ToArray())
            RemovePeer(peerId);
    }

    private void RemovePeer(Guid peerId)
    {
        if (!_peers.TryRemove(peerId, out var peerState))
            return;

        try { peerState.Dispose(); }
        catch (Exception ex)
        {
    #if DEBUG
            _logger.LogWarning(ex, "Error closing WebRTC peer {PeerId}.", peerId);
    #else
            _ = ex;
    #endif
        }

    #if DEBUG
        _logger.LogInformation("WebRTC peer removed {PeerId}. Active peers={PeerCount}.", peerId, _peers.Count);
    #endif
    }

    // ─────────────────────────────────────────────────────────────────────────
    private sealed class PeerState : IDisposable
    {
        private readonly RTCPeerConnection _peerConnection;
        private readonly int _payloadTypeId;
        private bool _hasBaseTimestamp;
        private long _baseCaptureTimestampUs;
        private uint _baseRtpTimestamp;

        public PeerState(RTCPeerConnection peerConnection, int payloadTypeId)
        {
            _peerConnection = peerConnection;
            _payloadTypeId  = payloadTypeId;
        }

        /// <summary>
        /// Sends one H.264 Access Unit (one or more NALs) via the RTP VideoStream.
        /// The VideoStream packetises into FU-A / STAP-A automatically.
        /// </summary>
        public bool TrySendNal(byte[] accessUnit, long captureTimestampUs)
        {
            try
            {
                var videoStream = _peerConnection.VideoStream;
                if (videoStream is null) return false;

                if (!_hasBaseTimestamp)
                {
                    _baseCaptureTimestampUs = captureTimestampUs;
                    _baseRtpTimestamp = (uint)Random.Shared.Next(0, int.MaxValue);
                    _hasBaseTimestamp = true;
                }

                long elapsedUs = Math.Max(0, captureTimestampUs - _baseCaptureTimestampUs);
                uint rtpTimestamp = _baseRtpTimestamp + (uint)((elapsedUs * H264ClockRate) / 1_000_000L);

                videoStream.SendH264Frame(rtpTimestamp, _payloadTypeId, accessUnit);
                return true;
            }
            catch
            {
                // Peer may have disconnected mid-send; the state-change event will clean up.
                return false;
            }
        }

        public void Dispose() => _peerConnection.close();
    }
}

