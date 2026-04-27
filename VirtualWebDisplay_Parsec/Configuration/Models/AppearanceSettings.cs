using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Configuration.Models;

public sealed class AppearanceSettings
{
    public string UiLanguage { get; set; } = "en";
    public string WindowTheme { get; set; } = WindowThemeOptions.System;

    public void EnsureValid()
    {
        UiLanguage = AppText.NormalizeLanguage(UiLanguage);
        WindowTheme = WindowThemeOptions.Normalize(WindowTheme);
    }
}
