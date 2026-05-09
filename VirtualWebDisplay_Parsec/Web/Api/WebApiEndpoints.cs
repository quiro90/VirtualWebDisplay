using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Streaming.Models;


namespace VirtualWebDisplay.Web.Api;

/// <summary>
/// Registra todos los endpoints HTTP de la aplicación en el <see cref="WebApplication"/>.
/// </summary>
internal static class WebApiEndpoints
{
    public static void Map(WebApplication app)
    {
        var orchestrator = app.Services.GetRequiredService<IWebEndpointOrchestrator>();

        app.MapPost("/auth/login", (HttpContext ctx, SecurityLoginRequest request) =>
            orchestrator.HandleAuthLogin(ctx, request));

        app.MapGet("/", (HttpContext ctx) =>
            orchestrator.HandleIndex(ctx));

        app.MapGet("/cap/{token}", (HttpContext ctx, string token) =>
            orchestrator.HandleCapture(ctx, token));

        app.MapGet("/mjpeg", (HttpContext ctx) =>
            orchestrator.HandleMjpeg(ctx));

        app.MapGet("/keepalive", (HttpContext ctx) =>
            orchestrator.HandleKeepalive(ctx));

        app.MapPost("/webrtc/offer", (HttpContext ctx, WebRtcSessionOffer offer, CancellationToken ct) =>
            orchestrator.HandleWebRtcOffer(ctx, offer, ct));

        app.MapPost("/input/touch", (HttpContext ctx, TouchInputRequest request) =>
            orchestrator.HandleTouchInput(ctx, request));

        app.MapGet("/input/stats", (HttpContext ctx) =>
            orchestrator.HandleTouchStats(ctx));

        app.MapGet("/cert", () =>
            orchestrator.HandleCert());

        app.MapGet("/config", (HttpContext ctx) =>
            orchestrator.HandleConfig(ctx));
    }
}
