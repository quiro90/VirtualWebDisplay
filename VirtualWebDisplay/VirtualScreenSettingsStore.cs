using System.Text.Json;

public sealed class VirtualScreenSettingsStore
{
    public const string FileName = "virtualscreen.user.json";

    private readonly string _filePath;

    public VirtualScreenSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, FileName);
    }

    public void Save(VirtualScreenConfig config)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var payload = new Dictionary<string, VirtualScreenConfig>
        {
            ["VirtualScreen"] = config,
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        File.WriteAllText(_filePath, json);
    }
}
