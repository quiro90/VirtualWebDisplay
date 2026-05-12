using VirtualWebDisplay.Web.Api;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class TouchInputCoordinateResolverTests
{
    [Fact]
    public void TryResolveDesktopCoordinates_ReturnsError_WhenCoordinatesAreMissing()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime();
        var request = new TouchInputRequest
        {
            Type = TouchInputActions.LegacyTouchMove,
            X = null,
            Y = null,
        };
        var resolver = new TouchInputCoordinateResolver();

        var result = resolver.TryResolveDesktopCoordinates(
            request,
            runtime,
            out var desktopX,
            out var desktopY,
            out var errorMessage);

        Assert.False(result);
        Assert.Equal(0, desktopX);
        Assert.Equal(0, desktopY);
        Assert.Equal(TouchInputCoordinateResolver.MissingCoordinatesError, errorMessage);
    }
}
