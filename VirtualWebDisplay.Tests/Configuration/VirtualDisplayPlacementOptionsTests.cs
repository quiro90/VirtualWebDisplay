using System.Drawing;
using VirtualWebDisplay.Configuration;

namespace VirtualWebDisplay.Tests.Configuration;

public sealed class VirtualDisplayPlacementOptionsTests
{
    [Theory]
    [InlineData(null, VirtualDisplayPlacementOptions.Right)]
    [InlineData("", VirtualDisplayPlacementOptions.Right)]
    [InlineData("left", VirtualDisplayPlacementOptions.Left)]
    [InlineData("up", VirtualDisplayPlacementOptions.Top)]
    [InlineData("down", VirtualDisplayPlacementOptions.Bottom)]
    [InlineData("duplicate", VirtualDisplayPlacementOptions.Duplicate)]
    [InlineData("unknown", VirtualDisplayPlacementOptions.Right)]
    public void Normalize_ReturnsExpectedPlacement(string? input, string expected)
    {
        var result = VirtualDisplayPlacementOptions.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("duplicate", true)]
    [InlineData(" DUPLICATE ", true)]
    [InlineData("left", false)]
    [InlineData(null, false)]
    public void IsDuplicate_DetectsDuplicatePlacement(string? input, bool expected)
    {
        Assert.Equal(expected, VirtualDisplayPlacementOptions.IsDuplicate(input));
    }

    [Fact]
    public void GetPosition_ReturnsExpectedCoordinatesForEveryPlacement()
    {
        var primary = new Rectangle(x: 100, y: 200, width: 300, height: 400);

        var right = VirtualDisplayPlacementOptions.GetPosition(primary, "right", width: 50, height: 60);
        var left = VirtualDisplayPlacementOptions.GetPosition(primary, "left", width: 50, height: 60);
        var top = VirtualDisplayPlacementOptions.GetPosition(primary, "top", width: 50, height: 60);
        var bottom = VirtualDisplayPlacementOptions.GetPosition(primary, "bottom", width: 50, height: 60);
        var duplicate = VirtualDisplayPlacementOptions.GetPosition(primary, "duplicate", width: 50, height: 60);

        Assert.Equal(new Point(400, 200), right);
        Assert.Equal(new Point(50, 200), left);
        Assert.Equal(new Point(100, 140), top);
        Assert.Equal(new Point(100, 600), bottom);
        Assert.Equal(new Point(100, 200), duplicate);
    }
}
