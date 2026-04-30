namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Tracks and limits the number of simultaneous viewers per screen.
/// Handles three viewer types: polling (web image), persistent MJPEG connections, and WebRTC peers.
/// MaxViewers == 0 means unlimited.
/// </summary>
public sealed class ViewerLimiter
{
    private static readonly TimeSpan PollingTimeout = TimeSpan.FromSeconds(15);

    private readonly int _maxViewers;
    private readonly Lock _lock = new();
    private int _mjpegCount;
    private readonly Dictionary<string, DateTimeOffset> _pollingViewers = new(StringComparer.Ordinal);

    /// <summary>Delegate wired to WebRtcStreamService.ActivePeerCount after construction.</summary>
    public Func<int> GetWebRtcCount { private get; set; } = () => 0;

    public ViewerLimiter(int maxViewers) => _maxViewers = Math.Max(0, maxViewers);

    public bool IsUnlimited => _maxViewers == 0;
    public int MaxViewers => _maxViewers;

    /// <summary>Total active viewers across all modes.</summary>
    public int ActiveCount
    {
        get
        {
            lock (_lock)
            {
                PrunePollingViewers();
                return _mjpegCount + GetWebRtcCount() + _pollingViewers.Count;
            }
        }
    }

    /// <summary>Checks whether one more viewer could enter without reserving a slot.</summary>
    public bool CanAcceptViewer()
    {
        if (IsUnlimited) return true;
        lock (_lock)
        {
            PrunePollingViewers();
            return _mjpegCount + GetWebRtcCount() + _pollingViewers.Count < _maxViewers;
        }
    }

    /// <summary>
    /// Registers or refreshes a polling viewer (web image / page-load slot).
    /// Returns true if the viewer is allowed (already has a slot or there is room), false if at capacity.
    /// </summary>
    public bool TryRegisterPolling(string viewerKey)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            PrunePollingViewers(now);

            if (_pollingViewers.ContainsKey(viewerKey))
            {
                _pollingViewers[viewerKey] = now;
                return true;
            }

            if (!IsUnlimited && _mjpegCount + GetWebRtcCount() + _pollingViewers.Count >= _maxViewers)
                return false;

            _pollingViewers[viewerKey] = now;
            return true;
        }
    }

    /// <summary>Checks whether a new WebRTC peer can be accepted without reserving a slot.</summary>
    public bool CanAcceptWebRtc()
    {
        return CanAcceptViewer();
    }

    /// <summary>Registers an MJPEG connection. Must be paired with ExitMjpeg() in a finally block.</summary>
    public bool TryEnterMjpeg()
    {
        if (IsUnlimited) return true;
        lock (_lock)
        {
            PrunePollingViewers();
            if (_mjpegCount + GetWebRtcCount() + _pollingViewers.Count >= _maxViewers)
                return false;
            _mjpegCount++;
            return true;
        }
    }

    public void ExitMjpeg()
    {
        lock (_lock)
        {
            if (_mjpegCount > 0) _mjpegCount--;
        }
    }

    private void PrunePollingViewers(DateTimeOffset? now = null)
    {
        var cutoff = (now ?? DateTimeOffset.UtcNow) - PollingTimeout;
        var toRemove = _pollingViewers.Keys.Where(k => _pollingViewers[k] < cutoff).ToList();
        foreach (var key in toRemove)
            _pollingViewers.Remove(key);
    }
}
