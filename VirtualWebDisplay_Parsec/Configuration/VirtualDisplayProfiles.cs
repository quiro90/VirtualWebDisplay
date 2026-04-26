using System.Drawing;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.Configuration;

public sealed record VirtualDisplayProfile(string Id, string DisplayName, int PortraitWidth, int PortraitHeight);

public static class VirtualDisplayProfiles
{
    public const string Custom = "Custom";

    private static readonly VirtualDisplayProfile[] Profiles =
    [
        new("1200x1920", "1200 Ã— 1920",           1200, 1920),
        new("1200x1800", "1200 Ã— 1800",           1200, 1800),
        new("1200x1600", "1200 Ã— 1600",           1200, 1600),
        new("1152x2048", "1152 Ã— 2048",           1152, 2048),
        new("1080x3840", "1080 Ã— 3840",           1080, 3840),
        new("1080x2560", "1080 Ã— 2560",           1080, 2560),
        new("1080x1920", "1080 Ã— 1920 (recomendada)", 1080, 1920),
        new("1050x1680", "1050 Ã— 1680",           1050, 1680),
        new("900x1600",  "900 Ã— 1600",             900, 1600),
        new("900x1440",  "900 Ã— 1440",             900, 1440),
        new("800x1280",  "800 Ã— 1280",             800, 1280),
        new("768x1366",  "768 Ã— 1366",             768, 1366),
        new("720x1280",  "720 Ã— 1280",             720, 1280),
        new(Custom,      "Personalizado",             0,    0),
    ];

    public static IReadOnlyList<VirtualDisplayProfile> All => Profiles;

    public static void EnsureValidSelection(VirtualScreenConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Profile))
        {
            if (TryMatch(config.Width, config.Height, out var profileId, out var landscape))
            {
                config.Profile = profileId;
                config.Landscape = landscape;
            }
            else
            {
                config.Profile = Custom;
                config.Landscape = config.Width > config.Height;
                config.CustomWidth = Math.Max(100, config.Landscape ? config.Height : config.Width);
                config.CustomHeight = Math.Max(100, config.Landscape ? config.Width : config.Height);
                return;
            }
        }

        if (IsCustom(config.Profile))
        {
            if (config.CustomWidth <= 0)
                config.CustomWidth = Math.Max(100, config.Landscape ? config.Height : config.Width);

            if (config.CustomHeight <= 0)
                config.CustomHeight = Math.Max(100, config.Landscape ? config.Width : config.Height);
        }

        var effectiveSize = GetEffectiveSize(config.Profile, config.Landscape, config.CustomWidth, config.CustomHeight);
        config.Width = effectiveSize.Width;
        config.Height = effectiveSize.Height;
    }

    public static Size GetEffectiveSize(string? profileId, bool landscape, int customWidth, int customHeight)
    {
        var portraitSize = GetPortraitSize(profileId, customWidth, customHeight);
        return landscape
            ? new Size(portraitSize.Height, portraitSize.Width)
            : portraitSize;
    }

    public static string NormalizeProfileId(string? profileId) =>
        All.FirstOrDefault(profile => string.Equals(profile.Id, profileId, StringComparison.OrdinalIgnoreCase))?.Id
        ?? Custom;

    public static string GetDisplayName(string? profileId) =>
        All.FirstOrDefault(profile => string.Equals(profile.Id, profileId, StringComparison.OrdinalIgnoreCase))?.DisplayName
        ?? "Personalizado";

    public static bool IsCustom(string? profileId) =>
        string.Equals(profileId, Custom, StringComparison.OrdinalIgnoreCase);

    private static Size GetPortraitSize(string? profileId, int customWidth, int customHeight)
    {
        var normalizedId = NormalizeProfileId(profileId);
        if (IsCustom(normalizedId))
            return new Size(Math.Max(100, customWidth), Math.Max(100, customHeight));

        var profile = Profiles.First(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
        return new Size(profile.PortraitWidth, profile.PortraitHeight);
    }

    private static bool TryMatch(int width, int height, out string profileId, out bool landscape)
    {
        foreach (var profile in Profiles.Where(profile => !IsCustom(profile.Id)))
        {
            if (profile.PortraitWidth == width && profile.PortraitHeight == height)
            {
                profileId = profile.Id;
                landscape = false;
                return true;
            }

            if (profile.PortraitWidth == height && profile.PortraitHeight == width)
            {
                profileId = profile.Id;
                landscape = true;
                return true;
            }
        }

        profileId = Custom;
        landscape = width > height;
        return false;
    }
}

