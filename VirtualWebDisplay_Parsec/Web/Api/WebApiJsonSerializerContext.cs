using System.Text.Json.Serialization;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Web.Api;

/// <summary>
/// Contexto de serialización JSON para los tipos de la API Web.
/// Este contexto permite la serialización sin reflexión para Native AOT.
/// </summary>
[JsonSerializable(typeof(SecurityLoginRequest))]
[JsonSerializable(typeof(WebRtcSessionOffer))]
[JsonSerializable(typeof(WebRtcSessionAnswer))]
[JsonSerializable(typeof(TouchInputRequest))]
[JsonSerializable(typeof(TouchStatsSnapshot))]
[JsonSerializable(typeof(VirtualScreenConfig))]
public partial class WebApiJsonSerializerContext : JsonSerializerContext
{
}
