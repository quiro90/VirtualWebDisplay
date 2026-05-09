using System.Reflection;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.Tests.Configuration;

/// <summary>
/// Guards that Clone() and CopyTo() copy every public settable property of VirtualScreenConfig.
/// If a new property is added to the class without updating Clone/CopyTo, these tests will fail.
/// </summary>
public sealed class VirtualScreenConfigCopyTests
{
    // ── Shared setup ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a VirtualScreenConfig with all settable public properties set to
    /// non-default values so tests can detect any omission in Clone/CopyTo.
    /// </summary>
    private static VirtualScreenConfig BuildNonDefaultConfig() => new()
    {
        Enabled                = false,
        Width                  = 1111,
        Height                 = 2222,
        Profile                = "test-profile",
        Landscape              = true,
        CustomWidth            = 3333,
        CustomHeight           = 4444,
        TransmissionMethod     = TransmissionModeOptions.WebImage,
        CaptureIntervalSeconds = 0.123,
        JpegQuality            = 77,
        ScreenSecurityEnabled  = true,
        MaxViewers             = 5,
        TouchInputEnabled      = true,
        TouchZoomEnabled       = false,
        TouchZoomDelayMs       = 100,
        TouchHoldEnabled       = false,
        TouchHoldDelayMs       = 420,
        TouchScrollEnabled     = false,
        TouchScrollDelayMs     = 420,
        TouchPreserveCursor    = true,
        Port                   = 9999,
        MonitorIndex           = 2,
        VirtualDisplayPlacement = "left",
        SavedPositionX         = 123,
        SavedPositionY         = 456,
        BrowserImageFit        = "cover",
        H264BitrateKbps        = 3500,
        H264Framerate          = 24,
    };

    // ── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Clone_CopiesAllPublicSettableProperties()
    {
        var source = BuildNonDefaultConfig();
        var clone  = source.Clone();

        AssertAllPropertiesEqual(source, clone);
    }

    [Fact]
    public void Clone_ReturnsNewInstance()
    {
        var source = BuildNonDefaultConfig();
        var clone  = source.Clone();

        Assert.NotSame(source, clone);
    }

    [Fact]
    public void CopyTo_CopiesAllPublicSettableProperties()
    {
        var source = BuildNonDefaultConfig();
        var target = new VirtualScreenConfig();

        source.CopyTo(target);

        AssertAllPropertiesEqual(source, target);
    }

    [Fact]
    public void Clone_And_CopyTo_CoverSameProperties()
    {
        // Cross-check: both methods must cover ALL public settable properties.
        // If a property exists in VirtualScreenConfig but is missing from BuildNonDefaultConfig,
        // this test surfaces it as a non-equal value between source and clone/target.
        var allProperties = GetPublicSettableProperties();
        var source        = BuildNonDefaultConfig();
        var defaults      = new VirtualScreenConfig();

        // Every property in BuildNonDefaultConfig must differ from the default value
        // so the copy assertion is meaningful (not a vacuous pass).
        var notOverridden = allProperties
            .Where(p => Equals(p.GetValue(source), p.GetValue(defaults)))
            .Select(p => p.Name)
            .ToList();

        Assert.True(
            notOverridden.Count == 0,
            $"BuildNonDefaultConfig does not override these properties — tests would pass vacuously: {string.Join(", ", notOverridden)}");
    }

    [Fact]
    public void Clone_CopiesSavedPositionFields()
    {
        var source = BuildNonDefaultConfig();

        var clone = source.Clone();

        Assert.Equal(source.SavedPositionX, clone.SavedPositionX);
        Assert.Equal(source.SavedPositionY, clone.SavedPositionY);
    }

    [Fact]
    public void CopyTo_CopiesSavedPositionFields()
    {
        var source = BuildNonDefaultConfig();
        var target = new VirtualScreenConfig();

        source.CopyTo(target);

        Assert.Equal(source.SavedPositionX, target.SavedPositionX);
        Assert.Equal(source.SavedPositionY, target.SavedPositionY);
    }

    [Fact]
    public void Clone_And_CopyTo_CopyH264Settings()
    {
        var source = BuildNonDefaultConfig();

        var clone = source.Clone();
        var target = new VirtualScreenConfig();
        source.CopyTo(target);

        Assert.Equal(source.H264BitrateKbps, clone.H264BitrateKbps);
        Assert.Equal(source.H264Framerate, clone.H264Framerate);
        Assert.Equal(source.H264BitrateKbps, target.H264BitrateKbps);
        Assert.Equal(source.H264Framerate, target.H264Framerate);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static void AssertAllPropertiesEqual(VirtualScreenConfig expected, VirtualScreenConfig actual)
    {
        foreach (var property in GetPublicSettableProperties())
        {
            var expectedValue = property.GetValue(expected);
            var actualValue   = property.GetValue(actual);
            Assert.True(
                Equals(expectedValue, actualValue),
                $"Property '{property.Name}' was not copied: expected '{expectedValue}', got '{actualValue}'. " +
                $"Update Clone() and/or CopyTo() in VirtualScreenConfig.");
        }
    }

    private static IReadOnlyList<PropertyInfo> GetPublicSettableProperties() =>
        typeof(VirtualScreenConfig)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();
}
