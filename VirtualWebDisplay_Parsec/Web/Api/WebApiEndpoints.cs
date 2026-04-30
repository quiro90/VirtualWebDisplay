using VirtualWebDisplay.Controllers.Handlers;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.UI.HtmlTemplates;


namespace VirtualWebDisplay.Controllers;

/// <summary>
/// Registra todos los endpoints HTTP de la aplicación en el <see cref="WebApplication"/>.
/// </summary>
internal static class WebApiEndpoints
{
    private static readonly WebImagePageTemplate    _webImageTemplate        = new();
    private static readonly RtcPageTemplate         _rtcTemplate             = new();
    private static readonly SecurityPageTemplate    _securityPageTemplate    = new();
    private static readonly ViewerLimitPageTemplate _viewerLimitPageTemplate = new();

    public static void Map(
        WebApplication app,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        byte[] tlsCertDerBytes)
    {
        app.MapPost("/auth/login", (HttpContext ctx, SecurityLoginRequest request) =>
            AuthHandler.HandleLogin(ctx, request, runtimes));

        app.MapGet("/", (HttpContext ctx) =>
            IndexHandler.HandleIndex(ctx, runtimes, _webImageTemplate, _rtcTemplate, _securityPageTemplate, _viewerLimitPageTemplate));

        app.MapGet("/cap", (HttpContext ctx) =>
            CaptureHandler.HandleCapture(ctx, runtimes));

        app.MapGet("/mjpeg", (HttpContext ctx) =>
            CaptureHandler.HandleMjpeg(ctx, runtimes));

        app.MapGet("/keepalive", (HttpContext ctx) =>
            KeepaliveHandler.HandleKeepalive(ctx, runtimes));

        app.MapPost("/webrtc/offer", (HttpContext ctx, WebRtcSessionOffer offer, CancellationToken ct) =>
            WebRtcHandler.HandleOffer(ctx, offer, runtimes, ct));

        app.MapPost("/input/touch", (HttpContext ctx, TouchInputRequest request) =>
            InputHandler.HandleTouchInput(ctx, request, runtimes));

        app.MapGet("/input/stats", (HttpContext ctx) =>
            InputHandler.HandleTouchStats(ctx, runtimes));

        app.MapGet("/cert", () =>
            Results.Bytes(tlsCertDerBytes, "application/x-x509-ca-cert", LocalCertificateProvider.CrtDownloadFileName));

        app.MapGet("/config", (HttpContext ctx) =>
            ConfigHandler.HandleConfig(ctx, runtimes));
    }
}
