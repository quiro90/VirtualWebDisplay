using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using System.Collections.Concurrent;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Streaming;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;

public sealed class WebRtcStreamService : BackgroundService, IAsyncDisposable
{
    private const int MaxChunkSize = 64 * 1024;
    private const int MaxBufferedAmount = 512 * 1024;
    private static readonly RTCConfiguration PeerConfiguration = new();

    private readonly CaptureService _captureService;
    private readonly VirtualScreenConfig _config;
    private readonly ILogger<WebRtcStreamService> _logger;
    private readonly ConcurrentDictionary<Guid, PeerState> _peers = new();
    private uint _frameId;

    public WebRtcStreamService(CaptureService captureService, VirtualScreenConfig config, ILogger<WebRtcStreamService> logger)
    {
        _captureService = captureService;
        _config = config;
        _logger = logger;
    }

    public int ActivePeerCount => _peers.Count;

    public async Task<WebRtcSessionAnswer> CreateAnswerAsync(WebRtcSessionOffer offer, CancellationToken cancellationToken)
    {        var peerId = Guid.NewGuid();
        var peerConnection = new RTCPeerConnection(PeerConfiguration);
        var iceGatheringComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        RTCDataChannel? framesChannel = null;

        peerConnection.onicecandidate += candidate =>
        {
            if (candidate is null || string.IsNullOrWhiteSpace(candidate.candidate))
                iceGatheringComplete.TrySetResult(true);
        };

        peerConnection.ondatachannel += channel =>
        {
            if (!string.Equals(channel.label, "frames", StringComparison.OrdinalIgnoreCase) || framesChannel is not null)
                return;

            framesChannel = channel;
            channel.onclose += () => RemovePeer(peerId);
        };

        peerConnection.onconnectionstatechange += state =>
        {
            if (state is RTCPeerConnectionState.closed or RTCPeerConnectionState.failed or RTCPeerConnectionState.disconnected)
                RemovePeer(peerId);
        };

        peerConnection.oniceconnectionstatechange += state =>
        {
            if (state is RTCIceConnectionState.closed or RTCIceConnectionState.failed or RTCIceConnectionState.disconnected)
                RemovePeer(peerId);
        };

        var setRemoteDescriptionResult = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
        {
            type = RTCSdpType.offer,
            sdp = offer.Sdp,
        });

        if (!string.Equals(setRemoteDescriptionResult.ToString(), "OK", StringComparison.OrdinalIgnoreCase))
        {
            peerConnection.close();
            throw new InvalidOperationException($"No se pudo aplicar la oferta WebRTC ({setRemoteDescriptionResult}).");
        }

        var answer = peerConnection.createAnswer(null);
        await peerConnection.setLocalDescription(answer);

        try
        {
            await iceGatheringComplete.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
        }
        catch (TimeoutException)
        {
        }

        _peers[peerId] = new PeerState(peerConnection, () => framesChannel);

        var localSdp = peerConnection.localDescription?.sdp?.ToString() ?? answer.sdp;
        return new WebRtcSessionAnswer(localSdp, "answer", peerId.ToString("N"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        byte[]? lastFrame = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Poll at a short interval so new frames are dispatched within ~10 ms
                // of being produced by CaptureService, regardless of the configured
                // capture interval. The actual frame-rate cap is enforced by CaptureService.
                if (_peers.Count > 0)
                {
                    var frame = _captureService.GetCurrentFrame();
                    if (frame.Length > 0 && !ReferenceEquals(frame, lastFrame))
                    {
                        lastFrame = frame;
                        var frameId = ++_frameId;foreach (var peerEntry in _peers.ToArray())
                            peerEntry.Value.TrySendFrame(frame, frameId);
                    }
                }

                await Task.Delay(10, stoppingToken);
            }
            catch (OperationCanceledException)
            {break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "No se pudo enviar un frame WebRTC.");
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        CloseAllPeers();
        await base.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
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

        try
        {
            peerState.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudo cerrar el peer WebRTC {PeerId}.", peerId);
        }
    }

    private sealed class PeerState : IDisposable
    {
        private readonly RTCPeerConnection _peerConnection;
        private readonly Func<RTCDataChannel?> _channelAccessor;

        public PeerState(RTCPeerConnection peerConnection, Func<RTCDataChannel?> channelAccessor)
        {
            _peerConnection = peerConnection;
            _channelAccessor = channelAccessor;
        }

        public bool TrySendFrame(byte[] frame, uint frameId)
        {
            var channel = _channelAccessor();
            if (channel is null || channel.readyState != RTCDataChannelState.open)
                return false;

            // Skip frame if send buffer is growing to avoid queuing stale frames.
            if (channel.bufferedAmount > MaxBufferedAmount)
                return false;

            channel.send($"{{\"type\":\"frame\",\"id\":{frameId},\"size\":{frame.Length}}}");

            // Prepend 4-byte little-endian frameId to each binary chunk so the
            // client can discard chunks that belong to a superseded frame.
            var idBytes = BitConverter.GetBytes(frameId);
            for (var offset = 0; offset < frame.Length; offset += MaxChunkSize)
            {
                var chunkLength = Math.Min(MaxChunkSize, frame.Length - offset);
                var chunk = new byte[4 + chunkLength];
                Buffer.BlockCopy(idBytes, 0, chunk, 0, 4);
                Buffer.BlockCopy(frame, offset, chunk, 4, chunkLength);
                channel.send(chunk);
            }

            return true;
        }

        public void Dispose()
        {
            _peerConnection.close();
        }
    }
}


