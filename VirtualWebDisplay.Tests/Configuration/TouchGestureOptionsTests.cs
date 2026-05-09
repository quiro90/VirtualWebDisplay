using VirtualWebDisplay.Configuration;

namespace VirtualWebDisplay.Tests.Configuration;

public sealed class TouchGestureOptionsTests
{
    [Fact]
    public void ClampDelay_ReturnsMin_WhenValueBelowMin()
    {
        var result = TouchGestureOptions.ClampDelay(TouchGestureOptions.MinDelayMs - 1);

        Assert.Equal(TouchGestureOptions.MinDelayMs, result);
    }

    [Fact]
    public void ClampDelay_ReturnsMax_WhenValueAboveMax()
    {
        var result = TouchGestureOptions.ClampDelay(TouchGestureOptions.MaxDelayMs + 1);

        Assert.Equal(TouchGestureOptions.MaxDelayMs, result);
    }

    [Fact]
    public void ClampDelay_ReturnsSameValue_WhenWithinRange()
    {
        const int expected = 250;

        var result = TouchGestureOptions.ClampDelay(expected);

        Assert.Equal(expected, result);
    }
}
