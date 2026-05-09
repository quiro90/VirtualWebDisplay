using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Web.Api;

internal interface IWebEndpointOrchestrator
{
    IResult HandleAuthLogin(HttpContext ctx, SecurityLoginRequest request);
    IResult HandleIndex(HttpContext ctx);
    IResult HandleCapture(HttpContext ctx, string token);
    Task HandleMjpeg(HttpContext ctx);
    IResult HandleKeepalive(HttpContext ctx);
    Task<IResult> HandleWebRtcOffer(HttpContext ctx, WebRtcSessionOffer offer, CancellationToken ct);
    IResult HandleTouchInput(HttpContext ctx, TouchInputRequest request);
    IResult HandleTouchStats(HttpContext ctx);
    IResult HandleCert();
    IResult HandleConfig(HttpContext ctx);
}

internal sealed class DefaultWebEndpointOrchestrator : IWebEndpointOrchestrator
{
    private readonly IReadOnlyList<ScreenRuntimeContext> _runtimes;
    private readonly byte[] _tlsCertDerBytes;
    private readonly WebImagePageTemplate _webImageTemplate = new();
    private readonly RtcPageTemplate _rtcTemplate = new();
    private readonly SecurityPageTemplate _securityPageTemplate = new();
    private readonly ViewerLimitPageTemplate _viewerLimitPageTemplate = new();

    public DefaultWebEndpointOrchestrator(
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        byte[] tlsCertDerBytes)
    {
        _runtimes = runtimes;
        _tlsCertDerBytes = tlsCertDerBytes;
    }

    public IResult HandleAuthLogin(HttpContext ctx, SecurityLoginRequest request) =>
        AuthHandler.HandleLogin(ctx, request, _runtimes);

    public IResult HandleIndex(HttpContext ctx) =>
        IndexHandler.HandleIndex(ctx, _runtimes, _webImageTemplate, _rtcTemplate, _securityPageTemplate, _viewerLimitPageTemplate);

    public IResult HandleCapture(HttpContext ctx, string token) =>
        CaptureHandler.HandleCapture(ctx, token, _runtimes);

    public Task HandleMjpeg(HttpContext ctx) =>
        CaptureHandler.HandleMjpeg(ctx, _runtimes);

    public IResult HandleKeepalive(HttpContext ctx) =>
        KeepaliveHandler.HandleKeepalive(ctx, _runtimes);

    public Task<IResult> HandleWebRtcOffer(HttpContext ctx, WebRtcSessionOffer offer, CancellationToken ct) =>
        WebRtcHandler.HandleOffer(ctx, offer, _runtimes, ct);

    public IResult HandleTouchInput(HttpContext ctx, TouchInputRequest request) =>
        InputHandler.HandleTouchInput(ctx, request, _runtimes);

    public IResult HandleTouchStats(HttpContext ctx) =>
        InputHandler.HandleTouchStats(ctx, _runtimes);

    public IResult HandleCert() =>
        Results.Bytes(_tlsCertDerBytes, "application/x-x509-ca-cert", LocalCertificateProvider.CrtDownloadFileName);

    public IResult HandleConfig(HttpContext ctx) =>
        ConfigHandler.HandleConfig(ctx, _runtimes);
}
