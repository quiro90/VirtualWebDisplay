using System.Text.Json.Serialization;

namespace VirtualWebDisplay.Configuration.Models;

/// <summary>
/// Define el modo de acceso a la red para la transmisión de la pantalla virtual.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NetworkAccessMode
{
    WiFi,
    USB
}