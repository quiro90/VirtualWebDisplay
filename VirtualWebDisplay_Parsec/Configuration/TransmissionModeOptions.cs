using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Configuration;

public static class TransmissionModeOptions
{
    public const string WebImage = "WebImage";
    public const string Rtc = "Rtc";

    private const double MinCaptureIntervalSeconds = 0.001;
    private const double MaxCaptureIntervalSeconds = 0.3;
    private const int MinJpegQuality = 10;
    private const int MaxJpegQuality = 100;

    public static void EnsureValidSelection(VirtualScreenConfig config)
    {
        config.TransmissionMethod = string.IsNullOrWhiteSpace(config.TransmissionMethod)
            ? GetRecommendedMethod(config.Profile)
            : NormalizeMethod(config.TransmissionMethod);
        config.CaptureIntervalSeconds = Math.Clamp(config.CaptureIntervalSeconds, MinCaptureIntervalSeconds, MaxCaptureIntervalSeconds);
        config.JpegQuality = Math.Clamp(config.JpegQuality, MinJpegQuality, MaxJpegQuality);
    }

    public static string GetRecommendedMethod(string? profileId) => Rtc;

    public static string NormalizeMethod(string? method) =>
        string.Equals(method, Rtc, StringComparison.OrdinalIgnoreCase) ? Rtc : WebImage;

    public static string GetDisplayName(string? method) =>
        NormalizeMethod(method) switch
        {
            Rtc => AppText.Get("Transmission_WebRtc"),
            _ => AppText.Get("Transmission_WebImage"),
        };

    public static bool IsWebImage(string? method) =>
        string.Equals(NormalizeMethod(method), WebImage, StringComparison.Ordinal);

    public static bool IsRtc(string? method) =>
        string.Equals(NormalizeMethod(method), Rtc, StringComparison.Ordinal);

    public static double GetEffectiveCaptureIntervalSeconds(VirtualScreenConfig config) =>
        Math.Clamp(config.CaptureIntervalSeconds, MinCaptureIntervalSeconds, MaxCaptureIntervalSeconds);

    public static int GetEffectiveJpegQuality(VirtualScreenConfig config) =>
        Math.Clamp(config.JpegQuality, MinJpegQuality, MaxJpegQuality);
}

