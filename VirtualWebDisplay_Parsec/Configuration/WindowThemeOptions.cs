namespace VirtualWebDisplay.Configuration;

public static class WindowThemeOptions
{
    public const string System = "system";
    public const string Light = "light";
    public const string Dark = "dark";

    public static string Normalize(string? theme)
    {
        return theme?.Trim().ToLowerInvariant() switch
        {
            Light => Light,
            Dark => Dark,
            _ => System,
        };
    }
}
