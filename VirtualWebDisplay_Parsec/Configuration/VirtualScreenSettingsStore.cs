using System.Text.Json;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.Configuration;

public sealed class VirtualScreenSettingsStore
{
    public const string DirectoryName = ".virtualwebdisplay";
    public const string FileName = "virtualscreen.user.json";
    private const string LegacySectionName = "VirtualScreen";

    private readonly string _filePath;

    public string FilePath => _filePath;

    public VirtualScreenSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? UserProfileFileHelper.GetFilePath(FileName);
    }

    public VirtualWebDisplaySettings Load()
    {
        var settings = UserProfileFileHelper.TryDeserialize<VirtualWebDisplaySettings>(_filePath);
        if (settings is not null)
        {
            settings.EnsureValid();
            return settings;
        }

        var legacy = UserProfileFileHelper.TryDeserialize<Dictionary<string, VirtualScreenConfig>>(_filePath);
        if (legacy is not null && legacy.TryGetValue(LegacySectionName, out var legacyConfig) && legacyConfig is not null)
        {
            var migrated = new VirtualWebDisplaySettings
            {
                Screen1 = legacyConfig,
                Screen2 = VirtualWebDisplaySettings.CreateScreen2Defaults(),
            };
            migrated.EnsureValid();
            return migrated;
        }

        return CreateDefaults();
    }

    public void Save(VirtualWebDisplaySettings settings)
    {
        settings.EnsureValid();
        var json = JsonSerializer.Serialize(settings, UserProfileFileHelper.JsonWriteOptions);
        UserProfileFileHelper.WriteAtomic(_filePath, json);
    }

    private static VirtualWebDisplaySettings CreateDefaults()
    {
        var defaults = new VirtualWebDisplaySettings();
        defaults.EnsureValid();
        return defaults;
    }
}
