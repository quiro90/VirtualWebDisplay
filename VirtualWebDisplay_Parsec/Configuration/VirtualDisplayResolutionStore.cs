using System.Text.Json;

namespace VirtualWebDisplay.Configuration;

/// <summary>
/// Persiste únicamente las resoluciones de los monitores virtuales en un fichero
/// independiente para lectura/escritura rápida sin tocar la config principal.
/// </summary>
public sealed class VirtualDisplayResolutionStore
{
    public const string FileName = "virtualscreen.display.json";

    private readonly string _filePath;

    public VirtualDisplayResolutionStore(string? filePath = null)
    {
        _filePath = filePath ?? UserProfileFileHelper.GetFilePath(FileName);
    }

    /// <summary>Carga el mapa de resoluciones. Clave: id del runtime ("screen1", "screen2").</summary>
    public Dictionary<string, (int Width, int Height, int X, int Y)> Load()
    {
        var raw = UserProfileFileHelper.TryDeserialize(
            _filePath,
            AppJsonSerializerContext.Default.DictionaryStringResolutionEntry);
        if (raw is null)
            return new();

        return raw
            .Where(kv => kv.Value.Width > 0 && kv.Value.Height > 0)
            .ToDictionary(kv => kv.Key, kv => (kv.Value.Width, kv.Value.Height, kv.Value.X, kv.Value.Y));
    }

    /// <summary>Guarda el mapa de resoluciones.</summary>
    public void Save(Dictionary<string, (int Width, int Height, int X, int Y)> resolutions)
    {
        var raw = resolutions.ToDictionary(
            kv => kv.Key,
            kv => new ResolutionEntry { Width = kv.Value.Width, Height = kv.Value.Height, X = kv.Value.X, Y = kv.Value.Y });

        var json = JsonSerializer.Serialize(raw, UserProfileFileHelper.JsonWriteOptions);
        UserProfileFileHelper.WriteAtomic(_filePath, json);
    }

    public sealed class ResolutionEntry
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
