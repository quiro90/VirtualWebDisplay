using VirtualWebDisplay.Web.Security;

namespace VirtualWebDisplay.Tests.Web.Security;

public sealed class ViewerLimiterTests
{
    [Fact]
    public void UnlimitedLimiter_AllowsAllEntries()
    {
        var limiter = new ViewerLimiter(maxViewers: 0)
        {
            GetWebRtcCount = () => 100,
        };

        var canAccept = limiter.CanAcceptViewer();
        var pollingOk = limiter.TryRegisterPolling("viewer-a");
        var mjpegOk = limiter.TryEnterMjpeg();

        Assert.True(limiter.IsUnlimited);
        Assert.True(canAccept);
        Assert.True(pollingOk);
        Assert.True(mjpegOk);
    }

    [Fact]
    public void TryRegisterPolling_ReusesExistingSlotForSameViewer()
    {
        var limiter = new ViewerLimiter(maxViewers: 1);

        var first = limiter.TryRegisterPolling("same-viewer");
        var second = limiter.TryRegisterPolling("same-viewer");

        Assert.True(first);
        Assert.True(second);
        Assert.Equal(1, limiter.ActiveCount);
    }

    [Fact]
    public void TryRegisterPolling_RejectsWhenAtCapacity()
    {
        var limiter = new ViewerLimiter(maxViewers: 1)
        {
            GetWebRtcCount = () => 1,
        };

        var allowed = limiter.TryRegisterPolling("viewer-a");

        Assert.False(allowed);
        Assert.Equal(1, limiter.ActiveCount);
    }

    [Fact]
    public void TryEnterMjpeg_RejectsWhenAtCapacity()
    {
        var limiter = new ViewerLimiter(maxViewers: 2)
        {
            GetWebRtcCount = () => 1,
        };

        var polling = limiter.TryRegisterPolling("viewer-a");
        var mjpeg = limiter.TryEnterMjpeg();

        Assert.True(polling);
        Assert.False(mjpeg);
        Assert.Equal(2, limiter.ActiveCount);
    }

    [Fact]
    public void ExitMjpeg_NeverDropsBelowZero()
    {
        var limiter = new ViewerLimiter(maxViewers: 1);

        limiter.ExitMjpeg();
        var entered = limiter.TryEnterMjpeg();
        limiter.ExitMjpeg();
        limiter.ExitMjpeg();

        Assert.True(entered);
        Assert.Equal(0, limiter.ActiveCount);
    }
}
