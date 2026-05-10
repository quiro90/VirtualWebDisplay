using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.Web.Services;

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
    private readonly IAuthService _authService;
    private readonly IConfigService _configService;
    private readonly IKeepaliveService _keepaliveService;
    private readonly ICaptureService _captureService;
    private readonly IWebRtcOfferService _webRtcOfferService;
    private readonly IInputService _inputService;
    private readonly IReadOnlyList<ScreenRuntimeContext> _runtimes;
    private readonly byte[] _tlsCertDerBytes;
    private readonly WebImagePageTemplate _webImageTemplate = new();
    private readonly RtcPageTemplate _rtcTemplate = new();
    private readonly SecurityPageTemplate _securityPageTemplate = new();
    private readonly ViewerLimitPageTemplate _viewerLimitPageTemplate = new();

    public DefaultWebEndpointOrchestrator(
        IAuthService authService,
        IConfigService configService,
        IKeepaliveService keepaliveService,
        ICaptureService captureService,
        IWebRtcOfferService webRtcOfferService,
        IInputService inputService,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        byte[] tlsCertDerBytes)
    {
        _authService = authService;
        _configService = configService;
        _keepaliveService = keepaliveService;
        _captureService = captureService;
        _webRtcOfferService = webRtcOfferService;
        _inputService = inputService;
        _runtimes = runtimes;
        _tlsCertDerBytes = tlsCertDerBytes;
    }

    public IResult HandleAuthLogin(HttpContext ctx, SecurityLoginRequest request) =>
        _authService.HandleLogin(ctx, request);

    public IResult HandleIndex(HttpContext ctx) =>
        IndexHandler.HandleIndex(ctx, _runtimes, _webImageTemplate, _rtcTemplate, _securityPageTemplate, _viewerLimitPageTemplate);

    public IResult HandleCapture(HttpContext ctx, string token) =>
        _captureService.HandleCapture(ctx, token);

    public Task HandleMjpeg(HttpContext ctx) =>
        _captureService.HandleMjpeg(ctx);

    public IResult HandleKeepalive(HttpContext ctx) =>
        _keepaliveService.HandleKeepalive(ctx);

    public Task<IResult> HandleWebRtcOffer(HttpContext ctx, WebRtcSessionOffer offer, CancellationToken ct) =>
        _webRtcOfferService.HandleOffer(ctx, offer, ct);

    public IResult HandleTouchInput(HttpContext ctx, TouchInputRequest request) =>
        _inputService.HandleTouchInput(ctx, request);

    public IResult HandleTouchStats(HttpContext ctx) =>
        _inputService.HandleTouchStats(ctx);

    public IResult HandleCert() =>
        Results.Bytes(_tlsCertDerBytes, "application/x-x509-ca-cert", LocalCertificateProvider.CrtDownloadFileName);

    public IResult HandleConfig(HttpContext ctx) =>
        _configService.HandleConfig(ctx);
}
