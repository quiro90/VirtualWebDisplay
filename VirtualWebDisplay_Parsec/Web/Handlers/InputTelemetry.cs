namespace VirtualWebDisplay.Web.Handlers;

internal sealed class InputTelemetry
{
    private long _totalEvents;
    private long _totalErrors;
    private long _rateLimitedEvents;
    private long _totalLatencyMs;
    private long _latencySamples;
    private long _lastInputUnixMs;
    private readonly Queue<long> _eventsWindowMs = new();
    private readonly object _statsLock = new();

    internal void RegisterEvent(long nowMs)
    {
        Interlocked.Increment(ref _totalEvents);
        Interlocked.Exchange(ref _lastInputUnixMs, nowMs);

        lock (_statsLock)
        {
            _eventsWindowMs.Enqueue(nowMs);
            PruneWindowLocked(nowMs);
        }
    }

    internal void RegisterError() => Interlocked.Increment(ref _totalErrors);

    internal void RegisterRateLimitedEvent() => Interlocked.Increment(ref _rateLimitedEvents);

    internal void RegisterLatency(long nowMs, long requestTimestamp)
    {
        if (requestTimestamp <= 0)
            return;

        var latency = nowMs - requestTimestamp;
        if (latency < 0 || latency > 60_000)
            return;

        Interlocked.Add(ref _totalLatencyMs, latency);
        Interlocked.Increment(ref _latencySamples);
    }

    internal TouchStatsSnapshot GetSnapshot(long nowMs) => new(
        TotalEvents: Interlocked.Read(ref _totalEvents),
        TotalErrors: Interlocked.Read(ref _totalErrors),
        RateLimitedEvents: Interlocked.Read(ref _rateLimitedEvents),
        EventsPerSecond: GetEventsPerSecond(nowMs),
        AverageLatencyMs: GetAverageLatencyMs(),
        LastInputAgoMs: GetLastInputAgoMs(nowMs));

    private int GetEventsPerSecond(long nowMs)
    {
        lock (_statsLock)
        {
            PruneWindowLocked(nowMs);
            return _eventsWindowMs.Count;
        }
    }

    private void PruneWindowLocked(long nowMs)
    {
        while (_eventsWindowMs.Count > 0 && nowMs - _eventsWindowMs.Peek() > 1000)
            _eventsWindowMs.Dequeue();
    }

    private double GetAverageLatencyMs()
    {
        var samples = Interlocked.Read(ref _latencySamples);
        if (samples <= 0)
            return 0;

        var totalLatency = Interlocked.Read(ref _totalLatencyMs);
        return Math.Round((double)totalLatency / samples, 1);
    }

    private long GetLastInputAgoMs(long nowMs)
    {
        var lastInput = Interlocked.Read(ref _lastInputUnixMs);
        if (lastInput <= 0)
            return -1;

        var delta = nowMs - lastInput;
        return delta < 0 ? 0 : delta;
    }
}
