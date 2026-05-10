using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Web.Services;

internal interface IAuthService
{
    IResult HandleLogin(HttpContext ctx, SecurityLoginRequest request);
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

internal interface IConfigService
{
    IResult HandleConfig(HttpContext ctx);
}

internal sealed class AuthService : IAuthService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public AuthService(IReadOnlyList<ScreenRuntimeContext> runtimes, IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleLogin(HttpContext ctx, SecurityLoginRequest request)
    {
        var runtime = _runtimeAccess.ResolveRuntime(ctx);

        if (!runtime.SecurityGate.Enabled)
            return _runtimeAccess.AuthorizedResult();

        var result = runtime.SecurityGate.TryAuthorize(ctx, _runtimeAccess.SecurityCookieName(runtime), request.Code);

        if (result.Authorized)
            return RuntimeAccessHelper.AuthorizedResult();

        if (result.TooManyAttempts)
        {
            return Results.Json(
                new
                {
                    error = AppText.Format("Program_Security_TooManyAttempts_Error", result.RetryAfterSeconds),
                    retryAfterSeconds = result.RetryAfterSeconds,
                    attemptsRemaining = 0,
                },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        return Results.Json(
            new
            {
                error = AppText.Get("Program_Security_InvalidCode_Error"),
                attemptsRemaining = result.AttemptsRemaining,
            },
            statusCode: StatusCodes.Status401Unauthorized);
    }
}

internal sealed class KeepaliveService : IKeepaliveService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public KeepaliveService(IReadOnlyList<ScreenRuntimeContext> runtimes, IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleKeepalive(HttpContext ctx)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out _, out var runtimeError))
            return runtimeError!;

        return Results.NoContent();
    }
}

internal sealed class ConfigService : IConfigService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public ConfigService(IReadOnlyList<ScreenRuntimeContext> runtimes, IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleConfig(HttpContext ctx)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out var runtime, out var runtimeError))
            return runtimeError!;

        return Results.Json(new
        {
            runtime.DisplayName,
            runtime.Config,
            runtime.HostUrl,
            runtime.IpUrl,
        });
    }
}

internal sealed class InputService : IInputService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public InputService(IReadOnlyList<ScreenRuntimeContext> runtimes, IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleTouchInput(HttpContext ctx, TouchInputRequest request)
    {
        return InputHandler.HandleTouchInput(ctx, request, _runtimeAccess);
    }

    public IResult HandleTouchStats(HttpContext ctx)
    {
        return InputHandler.HandleTouchStats(ctx, _runtimeAccess);
    }
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

internal sealed class CaptureService : ICaptureService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public CaptureService(IReadOnlyList<ScreenRuntimeContext> runtimes, IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleCapture(HttpContext ctx, string token)
    {
        var runtime = _runtimeAccess.ResolveRuntime(ctx);

        if (!string.Equals(token, runtime.CapToken, StringComparison.Ordinal))
            return _runtimeAccess.NotFoundResult();

        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out runtime, out var runtimeError))
            return runtimeError!;

        if (!runtime.ViewerLimiter.IsUnlimited)
        {
            var viewerKey = _runtimeAccess.ResolveViewerKey(ctx, runtime);
            if (!runtime.ViewerLimiter.TryRegisterPolling(viewerKey))
                return _runtimeAccess.ViewerLimitExceededResult();
        }

        runtime.FrameSource.NotifyJpegDemand();
        var frame = runtime.FrameSource.GetCurrentJpegFrame();
        if (frame.Length == 0)
            return RuntimeAccessHelper.ServiceUnavailableResult();

        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        return Results.Bytes(frame, "image/jpeg");
    }

    public async Task HandleMjpeg(HttpContext ctx)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out var runtime, out var runtimeError))
        {
            await runtimeError!.ExecuteAsync(ctx);
            return;
        }

        if (!runtime.ViewerLimiter.TryEnterMjpeg())
        {
            await _runtimeAccess.WriteViewerLimitExceededAsync(ctx);
            return;
        }

        runtime.FrameSource.EnterMjpegDemand();

        try
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
            ctx.Response.Headers.CacheControl = "no-store, no-cache";
            ctx.Response.Headers.Pragma = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            byte[]? lastFrame = null;
            var token = ctx.RequestAborted;

            while (!token.IsCancellationRequested)
            {
                var frame = runtime.FrameSource.GetCurrentJpegFrame();
                if (frame.Length == 0 || ReferenceEquals(frame, lastFrame))
                {
                    await Task.Delay(10, token);
                    continue;
                }

                lastFrame = frame;
                await ctx.Response.WriteAsync("--frame\r\n", token);
                await ctx.Response.WriteAsync("Content-Type: image/jpeg\r\n", token);
                await ctx.Response.WriteAsync($"Content-Length: {frame.Length}\r\n\r\n", token);
                await ctx.Response.Body.WriteAsync(frame, token);
                await ctx.Response.WriteAsync("\r\n", token);
                await ctx.Response.Body.FlushAsync(token);
            }
        }
        finally
        {
            runtime.FrameSource.ExitMjpegDemand();
            runtime.ViewerLimiter.ExitMjpeg();
        }
    }
}

internal sealed class WebRtcOfferService : IWebRtcOfferService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public WebRtcOfferService(IReadOnlyList<ScreenRuntimeContext> runtimes, IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public async Task<IResult> HandleOffer(HttpContext ctx, WebRtcSessionOffer offer, CancellationToken cancellationToken)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out var runtime, out var runtimeError))
            return runtimeError!;

        if (!TransmissionModeOptions.IsRtc(runtime.Config.TransmissionMethod))
            return _runtimeAccess.BadRequestError(AppText.Get("Program_WebRtcDisabled_Error"));

        if (!runtime.ViewerLimiter.CanAcceptWebRtc())
            return _runtimeAccess.ViewerLimitExceededResult();

        if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
            return _runtimeAccess.BadRequestError(AppText.Get("Program_WebRtcInvalidOffer_Error"));

        var answer = await runtime.WebRtcStreamService.CreateAnswerAsync(offer, cancellationToken);
        return Results.Json(answer);
    }
}
