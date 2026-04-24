using System.Drawing;

public sealed record VirtualDisplayProfile(string Id, string DisplayName, int PortraitWidth, int PortraitHeight);

public static class VirtualDisplayProfiles
{
    public const string Kindle = "Kindle";
    public const string KindlePaperWhite12 = "KindlePaperWhite12";
    public const string IPadMini = "IPadMini";
    public const string IPad = "IPad";
    public const string Custom = "Custom";

    private static readonly VirtualDisplayProfile[] Profiles =
    [
        CreateScaledProfile(Kindle, "Kindle", nativeWidth: 1072, nativeHeight: 1448, maxHeight: 900, reduceHeightBy13Percent: true),
        CreateScaledProfile(KindlePaperWhite12, "Kindle PaperWhite 12", nativeWidth: 1264, nativeHeight: 1680, maxHeight: 900, reduceHeightBy13Percent: true),
        CreateScaledProfile(IPadMini, "iPad Mini", nativeWidth: 1488, nativeHeight: 2266, maxHeight: 1300, reduceHeightBy13Percent: false),
        CreateScaledProfile(IPad, "iPad", nativeWidth: 1640, nativeHeight: 2360, maxHeight: 1800, reduceHeightBy13Percent: false),
        new(Custom, "Personalizado", 0, 0),
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

    private static VirtualDisplayProfile CreateScaledProfile(string id, string displayName, int nativeWidth, int nativeHeight, int maxHeight, bool reduceHeightBy13Percent)
    {
        var portraitHeight = reduceHeightBy13Percent
            ? (int)Math.Round(maxHeight * 0.87)
            : maxHeight;

        var portraitWidth = (int)Math.Round(portraitHeight * nativeWidth / (double)nativeHeight);
        return new VirtualDisplayProfile(id, displayName, portraitWidth, portraitHeight);
    }
}
