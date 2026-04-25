using System.Text.Json;

public sealed class VirtualScreenSettingsStore
{
    public const string DirectoryName = ".virtualwebdisplay";
    public const string FileName = "virtualscreen.user.json";
    private const string LegacySectionName = "VirtualScreen";

    private readonly string _filePath;

    public string FilePath => _filePath;

    public VirtualScreenSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            DirectoryName,
            FileName);
    }

    public VirtualWebDisplaySettings Load()
    {
        if (!File.Exists(_filePath))
            return CreateDefaults();

        try
        {
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<VirtualWebDisplaySettings>(json);
            if (settings is not null)
            {
                settings.EnsureValid();
                return settings;
            }

            var payload = JsonSerializer.Deserialize<Dictionary<string, VirtualScreenConfig>>(json);
            if (payload is not null && payload.TryGetValue(LegacySectionName, out var legacyConfig) && legacyConfig is not null)
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
        catch (IOException)
        {
            return CreateDefaults();
        }
        catch (UnauthorizedAccessException)
        {
            return CreateDefaults();
        }
        catch (JsonException)
        {
            return CreateDefaults();
        }
    }

    public void Save(VirtualWebDisplaySettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            EnsureHiddenDirectory(directory);

        settings.EnsureValid();

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        var tempFilePath = Path.Combine(directory ?? Path.GetTempPath(), $"{Path.GetFileName(_filePath)}.tmp");

        try
        {
            PrepareWritableFile(_filePath);
            File.WriteAllText(tempFilePath, json);

            if (File.Exists(_filePath))
                File.Replace(tempFilePath, _filePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            else
                File.Move(tempFilePath, _filePath);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        EnsureHiddenFile(_filePath);
    }

    private static void PrepareWritableFile(string filePath)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(filePath))
            return;

        var attributes = File.GetAttributes(filePath);
        var normalizedAttributes = attributes & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden & ~FileAttributes.System;
        if (normalizedAttributes != attributes)
            File.SetAttributes(filePath, normalizedAttributes);
    }

    private static VirtualWebDisplaySettings CreateDefaults()
    {
        var defaults = new VirtualWebDisplaySettings();
        defaults.EnsureValid();
        return defaults;
    }

    private static void EnsureHiddenDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
            return;

        var attributes = File.GetAttributes(directory);
        if ((attributes & FileAttributes.Hidden) == 0)
            File.SetAttributes(directory, attributes | FileAttributes.Hidden);
    }

    private static void EnsureHiddenFile(string filePath)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(filePath))
            return;

        var attributes = File.GetAttributes(filePath);
        if ((attributes & FileAttributes.Hidden) == 0)
            File.SetAttributes(filePath, attributes | FileAttributes.Hidden);
    }
}
