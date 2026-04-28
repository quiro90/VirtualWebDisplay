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
            ApplyLegacyTouchGestureHoldDelayMigration(settings);
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

    private void ApplyLegacyTouchGestureHoldDelayMigration(VirtualWebDisplaySettings settings)
    {
        // Migracion one-way desde versiones que almacenaban un valor global
        // TouchGestureHoldDelayMs en la raiz del JSON.
        if (!TryReadLegacyGlobalTouchGestureHoldDelay(out var legacyGlobalHoldDelay))
            return;

        var screen1IsDefault = settings.Screen1.TouchGestureHoldDelayMs == TouchGestureOptions.DefaultHoldDelayMs;
        var screen2IsDefault = settings.Screen2.TouchGestureHoldDelayMs == TouchGestureOptions.DefaultHoldDelayMs;

        if (!screen1IsDefault || !screen2IsDefault)
            return;

        var migrated = TouchGestureOptions.ClampHoldDelay(legacyGlobalHoldDelay);
        settings.Screen1.TouchGestureHoldDelayMs = migrated;
        settings.Screen2.TouchGestureHoldDelayMs = migrated;
    }

    private bool TryReadLegacyGlobalTouchGestureHoldDelay(out int holdDelayMs)
    {
        holdDelayMs = TouchGestureOptions.DefaultHoldDelayMs;

        try
        {
            if (!File.Exists(_filePath))
                return false;

            using var stream = File.OpenRead(_filePath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("TouchGestureHoldDelayMs", out var value))
                return false;

            if (!value.TryGetInt32(out holdDelayMs))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
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
