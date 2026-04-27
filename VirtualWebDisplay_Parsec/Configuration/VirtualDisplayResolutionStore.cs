using System.Text.Json;

namespace VirtualWebDisplay.Configuration;

/// <summary>
/// Persiste únicamente las resoluciones de los monitores virtuales en un fichero
/// independiente para lectura/escritura rápida sin tocar la config principal.
/// </summary>
public sealed class VirtualDisplayResolutionStore
{
    public const string FileName = "virtualscreen.display.json";

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public VirtualDisplayResolutionStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            VirtualScreenSettingsStore.DirectoryName,
            FileName);
    }

    /// <summary>Carga el mapa de resoluciones. Clave: id del runtime ("screen1", "screen2").</summary>
    public Dictionary<string, (int Width, int Height)> Load()
    {
        if (!File.Exists(_filePath))
            return new();

        try
        {
            var json = File.ReadAllText(_filePath);
            var raw = JsonSerializer.Deserialize<Dictionary<string, ResolutionEntry>>(json);
            if (raw is null)
                return new();

            return raw
                .Where(kv => kv.Value.Width > 0 && kv.Value.Height > 0)
                .ToDictionary(kv => kv.Key, kv => (kv.Value.Width, kv.Value.Height));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    /// <summary>Guarda el mapa de resoluciones.</summary>
    public void Save(Dictionary<string, (int Width, int Height)> resolutions)
    {
        var raw = resolutions.ToDictionary(
            kv => kv.Key,
            kv => new ResolutionEntry { Width = kv.Value.Width, Height = kv.Value.Height });

        var json = JsonSerializer.Serialize(raw, WriteOptions);
        UserProfileFileHelper.WriteAtomic(_filePath, json);
    }

    private sealed class ResolutionEntry
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
