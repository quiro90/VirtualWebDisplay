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
    private readonly IIndexPageService _indexPageService;
    private readonly IConfigService _configService;
    private readonly IKeepaliveService _keepaliveService;
    private readonly ICaptureService _captureService;
    private readonly IWebRtcOfferService _webRtcOfferService;
    private readonly IInputService _inputService;
    private readonly byte[] _tlsCertDerBytes;

    public DefaultWebEndpointOrchestrator(
        IAuthService authService,
        IIndexPageService indexPageService,
        IConfigService configService,
        IKeepaliveService keepaliveService,
        ICaptureService captureService,
        IWebRtcOfferService webRtcOfferService,
        IInputService inputService,
        byte[] tlsCertDerBytes)
    {
        _authService = authService;
        _indexPageService = indexPageService;
        _configService = configService;
        _keepaliveService = keepaliveService;
        _captureService = captureService;
        _webRtcOfferService = webRtcOfferService;
        _inputService = inputService;
        _tlsCertDerBytes = tlsCertDerBytes;
    }

    public IResult HandleAuthLogin(HttpContext ctx, SecurityLoginRequest request) =>
        _authService.HandleLogin(ctx, request);

    public IResult HandleIndex(HttpContext ctx) =>
        _indexPageService.HandleIndex(ctx);

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
