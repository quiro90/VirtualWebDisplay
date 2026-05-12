using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Web.Services;

internal interface IAuthService
{
    IResult HandleLogin(HttpContext ctx, SecurityLoginRequest request);
}

internal interface IIndexPageService
{
    IResult HandleIndex(HttpContext ctx);
}

internal interface IConfigService
{
    IResult HandleConfig(HttpContext ctx);
}

internal interface IKeepaliveService
{
    IResult HandleKeepalive(HttpContext ctx);
}

internal interface IInputService
{
    IResult HandleTouchInput(HttpContext ctx, TouchInputRequest request);
    IResult HandleTouchStats(HttpContext ctx);
}

internal interface ICaptureService
{
    IResult HandleCapture(HttpContext ctx, string token);
    Task HandleMjpeg(HttpContext ctx);
}

internal interface IWebRtcOfferService
{
    Task<IResult> HandleOffer(HttpContext ctx, WebRtcSessionOffer offer, CancellationToken cancellationToken);
}
