using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class TouchInputActionsTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("DragStart", "dragstart")]
    [InlineData("SCROLLEND", "scrollend")]
    public void NormalizeAction_ReturnsLowercaseOrEmpty(string? value, string expected)
    {
        Assert.Equal(expected, TouchInputActions.NormalizeAction(value));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("TouchStart", "touchstart")]
    [InlineData("TOUCHEND", "touchend")]
    public void NormalizeLegacyType_ReturnsLowercaseOrEmpty(string? value, string expected)
    {
        Assert.Equal(expected, TouchInputActions.NormalizeLegacyType(value));
    }

    [Theory]
    [InlineData(TouchInputActions.DragStart)]
    [InlineData(TouchInputActions.DragMove)]
    [InlineData(TouchInputActions.DragEnd)]
    public void IsDragAction_ReturnsTrue_ForDragActions(string action)
    {
        Assert.True(TouchInputActions.IsDragAction(action));
    }

    [Theory]
    [InlineData(TouchInputActions.ScrollMove)]
    [InlineData(TouchInputActions.ScrollEnd)]
    public void IsScrollAction_ReturnsTrue_ForScrollActions(string action)
    {
        Assert.True(TouchInputActions.IsScrollAction(action));
    }

    [Theory]
    [InlineData(TouchInputActions.DragEnd)]
    [InlineData(TouchInputActions.ScrollEnd)]
    public void IsGestureEndAction_ReturnsTrue_ForTerminalActions(string action)
    {
        Assert.True(TouchInputActions.IsGestureEndAction(action));
    }
}
