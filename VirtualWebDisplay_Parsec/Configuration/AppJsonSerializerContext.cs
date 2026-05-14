using System.Text.Json.Serialization;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.Configuration;

/// <summary>
/// Contexto de serialización JSON para Native AOT.
/// Este contexto permite la serialización sin reflexión de las clases de configuración.
/// </summary>
[JsonSerializable(typeof(VirtualWebDisplaySettings))]
[JsonSerializable(typeof(VirtualScreenConfig))]
[JsonSerializable(typeof(AppearanceSettings))]
[JsonSerializable(typeof(Dictionary<string, VirtualScreenConfig>))]
[JsonSerializable(typeof(Dictionary<string, VirtualDisplayResolutionStore.ResolutionEntry>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
