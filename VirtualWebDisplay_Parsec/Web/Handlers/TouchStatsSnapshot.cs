namespace VirtualWebDisplay.Web.Handlers;

public readonly record struct TouchStatsSnapshot(
    long TotalEvents,
    long TotalErrors,
    long RateLimitedEvents,
    int EventsPerSecond,
    double AverageLatencyMs,
    long LastInputAgoMs);
