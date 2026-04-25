using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;

public sealed class WebRtcStreamService : BackgroundService, IAsyncDisposable
{
    private const int MaxChunkSize = 16 * 1024;
    private static readonly RTCConfiguration PeerConfiguration = new();

    private readonly CaptureService _captureService;
    private readonly VirtualScreenConfig _config;
    private readonly ILogger<WebRtcStreamService> _logger;
    private readonly ConcurrentDictionary<Guid, PeerState> _peers = new();

    public WebRtcStreamService(CaptureService captureService, VirtualScreenConfig config, ILogger<WebRtcStreamService> logger)
    {
        _captureService = captureService;
        _config = config;
        _logger = logger;
    }

    public async Task<WebRtcSessionAnswer> CreateAnswerAsync(WebRtcSessionOffer offer, CancellationToken cancellationToken)
    {
        var peerId = Guid.NewGuid();
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
                var frame = _captureService.GetCurrentFrame();
                if (frame.Length > 0 && !ReferenceEquals(frame, lastFrame))
                {
                    lastFrame = frame;

                    foreach (var peerEntry in _peers.ToArray())
                    {
                        if (!peerEntry.Value.TrySendFrame(frame))
                            continue;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(TransmissionModeOptions.GetEffectiveCaptureIntervalSeconds(_config)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
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
        foreach (var peerId in _peers.Keys.ToArray())
            RemovePeer(peerId);

        await base.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var peerId in _peers.Keys.ToArray())
            RemovePeer(peerId);

        await base.StopAsync(CancellationToken.None);
        base.Dispose();
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

        public bool TrySendFrame(byte[] frame)
        {
            var channel = _channelAccessor();
            if (channel is null || channel.readyState != RTCDataChannelState.open)
                return false;

            channel.send($"{{\"type\":\"frame\",\"size\":{frame.Length}}}");

            for (var offset = 0; offset < frame.Length; offset += MaxChunkSize)
            {
                var chunkLength = Math.Min(MaxChunkSize, frame.Length - offset);
                var chunk = new byte[chunkLength];
                Buffer.BlockCopy(frame, offset, chunk, 0, chunkLength);
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

public sealed record WebRtcSessionOffer(string Sdp, string Type);
public sealed record WebRtcSessionAnswer(string Sdp, string Type, string PeerId);
