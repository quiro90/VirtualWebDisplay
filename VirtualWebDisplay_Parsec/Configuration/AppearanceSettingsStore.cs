using System.Globalization;
using System.Text.Json;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.Configuration;

public sealed class AppearanceSettingsStore
{
    public const string FileName = "ui-preferences.user.json";

    private readonly string _filePath;

    public string FilePath => _filePath;

    public AppearanceSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? UserProfileFileHelper.GetFilePath(FileName);
    }

    public AppearanceSettings Load()
    {
        var settings = UserProfileFileHelper.TryDeserialize(
            _filePath,
            AppJsonSerializerContext.Default.AppearanceSettings);

        if (settings is not null)
        {
            settings.EnsureValid();
            return settings;
        }

        return CreateDefaults();
    }

    public void Save(AppearanceSettings settings)
    {
        settings.EnsureValid();
        var json = JsonSerializer.Serialize(settings, UserProfileFileHelper.JsonWriteOptions);
        UserProfileFileHelper.WriteAtomic(_filePath, json);
    }

    private static AppearanceSettings CreateDefaults()
    {
        var defaults = new AppearanceSettings
        {
            UiLanguage = DetectSystemLanguage(),
        };
        defaults.EnsureValid();
        return defaults;
    }

    /// <summary>
    /// Returns "es" for any Spanish locale (es-ES, es-MX, es-AR, etc.), "en" for everything else.
    /// </summary>
    private static string DetectSystemLanguage()
    {
        var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return string.Equals(twoLetter, "es", StringComparison.OrdinalIgnoreCase) ? "es" : "en";
    }
}
