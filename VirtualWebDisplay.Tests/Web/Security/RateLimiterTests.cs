using VirtualWebDisplay.Web.Security;

namespace VirtualWebDisplay.Tests.Web.Security;

public sealed class RateLimiterTests
{
    [Fact]
    public void AllowRequest_ReturnsTrue_WhenTokensAvailable()
    {
        var limiter = new RateLimiter(maxEventsPerSecond: 5);

        Assert.True(limiter.AllowRequest());
    }

    [Fact]
    public void AllowRequest_AllowsUpToMaxPerSecond()
    {
        const int max = 10;
        var limiter = new RateLimiter(maxEventsPerSecond: max);

        var allowed = 0;
        for (var i = 0; i < max; i++)
            if (limiter.AllowRequest()) allowed++;

        Assert.Equal(max, allowed);
    }

    [Fact]
    public void AllowRequest_ReturnsFalse_WhenBucketExhausted()
    {
        var limiter = new RateLimiter(maxEventsPerSecond: 3);

        // Drain all tokens
        limiter.AllowRequest();
        limiter.AllowRequest();
        limiter.AllowRequest();

        Assert.False(limiter.AllowRequest());
    }

    [Fact]
    public void Reset_RefilsBucket()
    {
        var limiter = new RateLimiter(maxEventsPerSecond: 2);

        limiter.AllowRequest();
        limiter.AllowRequest();
        Assert.False(limiter.AllowRequest()); // empty

        limiter.Reset();

        Assert.True(limiter.AllowRequest()); // replenished
    }

    [Fact]
    public void GetStatus_ReflectsConsumedTokens()
    {
        var limiter = new RateLimiter(maxEventsPerSecond: 5);

        limiter.AllowRequest();
        limiter.AllowRequest();

        var (tokens, max, _) = limiter.GetStatus();

        Assert.Equal(5, max);
        Assert.Equal(3, tokens);
    }

    [Fact]
    public void GetStatus_MaxPerSecond_MatchesConstructorArg()
    {
        var limiter = new RateLimiter(maxEventsPerSecond: 42);

        var (_, max, _) = limiter.GetStatus();

        Assert.Equal(42, max);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_ClampsInvalidMax_ToOne(int badMax)
    {
        var limiter = new RateLimiter(maxEventsPerSecond: badMax);

        var (_, max, _) = limiter.GetStatus();

        Assert.Equal(1, max);
        Assert.True(limiter.AllowRequest()); // first request allowed
        Assert.False(limiter.AllowRequest()); // immediately limited
    }

    [Fact]
    public void AllowRequest_IsThreadSafe()
    {
        const int max = 100;
        var limiter = new RateLimiter(maxEventsPerSecond: max);
        var allowed = 0;

        Parallel.For(0, max * 2, _ =>
        {
            if (limiter.AllowRequest())
                Interlocked.Increment(ref allowed);
        });

        Assert.True(allowed <= max, $"Expected <= {max} but got {allowed}");
        Assert.True(allowed > 0);
    }
}
