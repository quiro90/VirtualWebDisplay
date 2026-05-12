namespace VirtualWebDisplay.Web.Handlers;

internal sealed class DragStateTracker
{
    private readonly int _staleTimeoutMs;
    private readonly object _lock = new();
    private bool _isActive;
    private long _lastActivityUnixMs;

    internal DragStateTracker(int staleTimeoutMs)
    {
        _staleTimeoutMs = staleTimeoutMs;
    }

    internal void MarkStarted(long nowMs)
    {
        lock (_lock)
        {
            _isActive = true;
            _lastActivityUnixMs = nowMs;
        }
    }

    internal void MarkActivity(long nowMs)
    {
        lock (_lock)
        {
            if (_isActive)
                _lastActivityUnixMs = nowMs;
        }
    }

    internal bool TryEnd()
    {
        lock (_lock)
        {
            var shouldRelease = _isActive;
            _isActive = false;
            _lastActivityUnixMs = 0;
            return shouldRelease;
        }
    }

    internal bool TryReleaseIfStale(long nowMs)
    {
        lock (_lock)
        {
            var shouldRelease = _isActive && (nowMs - _lastActivityUnixMs) >= _staleTimeoutMs;
            if (!shouldRelease)
                return false;

            _isActive = false;
            _lastActivityUnixMs = 0;
            return true;
        }
    }
}
