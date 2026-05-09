using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class InputCoordinateMapperTests
{
    // ── Centre mapping ───────────────────────────────────────────────────────

    [Fact]
    public void Map_CentreOfViewport_MapsToCentreOfScreen()
    {
        var (x, y) = InputCoordinateMapper.Map(500, 400, 1000, 800, 1920, 1080);

        // Centre: ~960, ~540
        Assert.InRange(x, 958, 962);
        Assert.InRange(y, 538, 542);
    }

    // ── Origin / corners ─────────────────────────────────────────────────────

    [Fact]
    public void Map_OriginCoordinates_MapsToZeroZero()
    {
        var (x, y) = InputCoordinateMapper.Map(0, 0, 1920, 1080, 1920, 1080);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void Map_FullViewport_MapsToMaxScreenPixel()
    {
        var (x, y) = InputCoordinateMapper.Map(1920, 1080, 1920, 1080, 1920, 1080);

        Assert.Equal(1919, x);
        Assert.Equal(1079, y);
    }

    [Fact]
    public void Map_TopRight_MapsCorrectly()
    {
        var (x, y) = InputCoordinateMapper.Map(1920, 0, 1920, 1080, 1920, 1080);

        Assert.Equal(1919, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void Map_BottomLeft_MapsCorrectly()
    {
        var (x, y) = InputCoordinateMapper.Map(0, 1080, 1920, 1080, 1920, 1080);

        Assert.Equal(0, x);
        Assert.Equal(1079, y);
    }

    // ── Clamping ──────────────────────────────────────────────────────────────

    [Fact]
    public void Map_NegativeViewportX_ClampsToZero()
    {
        var (x, _) = InputCoordinateMapper.Map(-50, 0, 1920, 1080, 1920, 1080);

        Assert.Equal(0, x);
    }

    [Fact]
    public void Map_NegativeViewportY_ClampsToZero()
    {
        var (_, y) = InputCoordinateMapper.Map(0, -50, 1920, 1080, 1920, 1080);

        Assert.Equal(0, y);
    }

    [Fact]
    public void Map_ViewportExceedsWidth_ClampsToMaxX()
    {
        var (x, _) = InputCoordinateMapper.Map(9999, 0, 1920, 1080, 1920, 1080);

        Assert.Equal(1919, x);
    }

    [Fact]
    public void Map_ViewportExceedsHeight_ClampsToMaxY()
    {
        var (_, y) = InputCoordinateMapper.Map(0, 9999, 1920, 1080, 1920, 1080);

        Assert.Equal(1079, y);
    }

    // ── Degenerate viewport dimensions ────────────────────────────────────────

    [Fact]
    public void Map_ZeroViewportWidth_ReturnsZeroX()
    {
        var (x, _) = InputCoordinateMapper.Map(500, 400, 0, 800, 1920, 1080);

        Assert.Equal(0, x);
    }

    [Fact]
    public void Map_ZeroViewportHeight_ReturnsZeroY()
    {
        var (_, y) = InputCoordinateMapper.Map(500, 400, 1000, 0, 1920, 1080);

        Assert.Equal(0, y);
    }

    [Fact]
    public void Map_ScreenWidthOne_AlwaysReturnsZeroX()
    {
        var (x, _) = InputCoordinateMapper.Map(500, 0, 1000, 1000, 1, 1080);

        Assert.Equal(0, x);
    }

    [Fact]
    public void Map_ScreenHeightOne_AlwaysReturnsZeroY()
    {
        var (_, y) = InputCoordinateMapper.Map(0, 500, 1000, 1000, 1920, 1);

        Assert.Equal(0, y);
    }

    // ── Aspect ratio independence ─────────────────────────────────────────────

    [Fact]
    public void Map_DifferentViewportAndScreenAspect_ScalesIndependently()
    {
        // Viewport 100x200, screen 400x100 → centre should map to ~200, ~50
        var (x, y) = InputCoordinateMapper.Map(50, 100, 100, 200, 400, 100);

        Assert.InRange(x, 198, 202);
        Assert.InRange(y, 48, 52);
    }

    // ── Rounding behaviour ────────────────────────────────────────────────────

    [Fact]
    public void Map_OnePixelRightOfCentre_RoundsCorrectly()
    {
        // screen 100x100, viewport exactly at pixel 51/100
        var (x, _) = InputCoordinateMapper.Map(51, 0, 100, 100, 100, 1);

        // 51/100 * 99 = 50.49 → rounds to 50
        Assert.Equal(50, x);
    }
}
