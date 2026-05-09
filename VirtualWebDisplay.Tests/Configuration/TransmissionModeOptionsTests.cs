using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.Tests.Configuration;

public sealed class TransmissionModeOptionsTests
{
    [Theory]
    [InlineData(null, TransmissionModeOptions.WebImage)]
    [InlineData("", TransmissionModeOptions.WebImage)]
    [InlineData("webimage", TransmissionModeOptions.WebImage)]
    [InlineData("Rtc", TransmissionModeOptions.Rtc)]
    [InlineData("rtc", TransmissionModeOptions.Rtc)]
    [InlineData("unknown", TransmissionModeOptions.WebImage)]
    public void NormalizeMethod_ReturnsExpectedValue(string? input, string expected)
    {
        var result = TransmissionModeOptions.NormalizeMethod(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void EnsureValidSelection_NormalizesMethodAndClampsRanges()
    {
        var config = new VirtualScreenConfig
        {
            TransmissionMethod = "",
            CaptureIntervalSeconds = 10,
            JpegQuality = 999,
        };

        TransmissionModeOptions.EnsureValidSelection(config);

        Assert.Equal(TransmissionModeOptions.Rtc, config.TransmissionMethod);
        Assert.Equal(0.3, config.CaptureIntervalSeconds);
        Assert.Equal(100, config.JpegQuality);
    }

    [Fact]
    public void EnsureValidSelection_ClampsLowerBounds()
    {
        var config = new VirtualScreenConfig
        {
            TransmissionMethod = TransmissionModeOptions.WebImage,
            CaptureIntervalSeconds = 0,
            JpegQuality = 0,
        };

        TransmissionModeOptions.EnsureValidSelection(config);

        Assert.Equal(0.001, config.CaptureIntervalSeconds);
        Assert.Equal(10, config.JpegQuality);
    }

    [Fact]
    public void GetEffectiveValues_ClampWithoutMutatingSource()
    {
        var config = new VirtualScreenConfig
        {
            CaptureIntervalSeconds = -1,
            JpegQuality = 1000,
        };

        var interval = TransmissionModeOptions.GetEffectiveCaptureIntervalSeconds(config);
        var quality = TransmissionModeOptions.GetEffectiveJpegQuality(config);

        Assert.Equal(0.001, interval);
        Assert.Equal(100, quality);
        Assert.Equal(-1, config.CaptureIntervalSeconds);
        Assert.Equal(1000, config.JpegQuality);
    }

    [Theory]
    [InlineData(TransmissionModeOptions.WebImage, true, false)]
    [InlineData(TransmissionModeOptions.Rtc, false, true)]
    [InlineData("invalid", true, false)]
    public void ModePredicates_WorkAsExpected(string method, bool isWebImage, bool isRtc)
    {
        Assert.Equal(isWebImage, TransmissionModeOptions.IsWebImage(method));
        Assert.Equal(isRtc, TransmissionModeOptions.IsRtc(method));
    }
}
