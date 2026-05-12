namespace VirtualWebDisplay.Web.Handlers;

internal sealed class RateLimiterRegistry
{
    private readonly int _maxEventsPerSecond;
    private readonly Dictionary<string, RateLimiter> _rateLimiters = new();
    private readonly object _lock = new();

    internal RateLimiterRegistry(int maxEventsPerSecond)
    {
        _maxEventsPerSecond = maxEventsPerSecond;
    }

    internal bool AllowRequest(string viewerKey)
    {
        if (string.IsNullOrEmpty(viewerKey))
            viewerKey = "default";

        lock (_lock)
        {
            if (!_rateLimiters.TryGetValue(viewerKey, out var limiter))
            {
                limiter = new RateLimiter(_maxEventsPerSecond);
                _rateLimiters[viewerKey] = limiter;
            }

            return limiter.AllowRequest();
        }
    }
}
