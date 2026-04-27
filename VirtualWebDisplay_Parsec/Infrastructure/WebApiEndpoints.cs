using System.Net;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Controllers;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.UI.HtmlTemplates;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Registra todos los endpoints HTTP de la aplicación en el <see cref="WebApplication"/>.
/// </summary>
internal static class WebApiEndpoints
{
    private static readonly WebImagePageTemplate   _webImageTemplate        = new();
    private static readonly RtcPageTemplate        _rtcTemplate             = new();
    private static readonly SecurityPageTemplate   _securityPageTemplate    = new();
    private static readonly ViewerLimitPageTemplate _viewerLimitPageTemplate = new();

    public static void Map(
        WebApplication app,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        byte[] tlsCertDerBytes)
    {
        app.MapPost("/auth/login", (HttpContext ctx, SecurityLoginRequest request) =>
        {
            var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
            if (!runtime.SecurityGate.Enabled)
                return Results.Ok(new { authorized = true });

            var result = runtime.SecurityGate.TryAuthorize(ctx, RuntimeAccessHelper.SecurityCookieName(runtime), request.Code);
            if (result.Authorized)
                return Results.Ok(new { authorized = true });

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
        });

        app.MapGet("/", (HttpContext ctx) =>
        {
            var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
            var isAuthorized = RuntimeAccessHelper.IsAuthorized(ctx, runtime);

            if (!runtime.ViewerLimiter.IsUnlimited)
            {
                if (TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod))
                {
                    var canContinue = isAuthorized
                        ? runtime.ViewerLimiter.TryRegisterPolling(RuntimeAccessHelper.ResolveViewerKey(ctx, runtime))
                        : runtime.ViewerLimiter.CanAcceptViewer();

                    if (!canContinue)
                        return Results.Content(_viewerLimitPageTemplate.Generate(runtime), "text/html");
                }
                else
                {
                    if (!runtime.ViewerLimiter.CanAcceptViewer())
                        return Results.Content(_viewerLimitPageTemplate.Generate(runtime), "text/html");
                }
            }

            if (!isAuthorized)
                return Results.Content(_securityPageTemplate.Generate(runtime, ctx), "text/html");

            var browserImageFit = RuntimeAccessHelper.NormalizeBrowserImageFit(runtime.Config.BrowserImageFit);

            string html;
            if (TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod))
            {
                var parameters = new Dictionary<string, object>
                {
                    ["title"]          = runtime.DisplayName,
                    ["browserImageFit"] = browserImageFit,
                    ["intervalMs"]     = Math.Max(3, (int)Math.Round(runtime.Config.CaptureIntervalSeconds * 1000))
                };
                html = _webImageTemplate.Generate(parameters);
            }
            else
            {
                var parameters = new Dictionary<string, object>
                {
                    ["title"]          = runtime.DisplayName,
                    ["browserImageFit"] = browserImageFit
                };
                html = _rtcTemplate.Generate(parameters);
            }

            return Results.Content(html, "text/html");
        });

        app.MapGet("/cap", (HttpContext ctx) =>
        {
            var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
            if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
                return RuntimeAccessHelper.UnauthorizedResult(runtime);

            if (!runtime.ViewerLimiter.IsUnlimited)
            {
                var viewerKey = RuntimeAccessHelper.ResolveViewerKey(ctx, runtime);
                if (!runtime.ViewerLimiter.TryRegisterPolling(viewerKey))
                    return Results.Json(
                        new { error = AppText.Get("Program_ViewerLimit_Full_Error") },
                        statusCode: StatusCodes.Status429TooManyRequests);
            }

            var frame = runtime.CaptureService.GetCurrentFrame();
            if (frame.Length == 0)
                return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable);

            ctx.Response.Headers.CacheControl = "no-store, no-cache";
            return Results.Bytes(frame, "image/jpeg");
        });

        app.MapPost("/webrtc/offer", async (HttpContext ctx, WebRtcSessionOffer offer, CancellationToken cancellationToken) =>
        {
            var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
            if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
                return RuntimeAccessHelper.UnauthorizedResult(runtime);

            if (!TransmissionModeOptions.IsRtc(runtime.Config.TransmissionMethod))
                return Results.BadRequest(new { error = AppText.Get("Program_WebRtcDisabled_Error") });

            if (!runtime.ViewerLimiter.CanAcceptWebRtc())
                return Results.Json(
                    new { error = AppText.Get("Program_ViewerLimit_Full_Error") },
                    statusCode: StatusCodes.Status429TooManyRequests);

            if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
                return Results.BadRequest(new { error = AppText.Get("Program_WebRtcInvalidOffer_Error") });

            var answer = await runtime.WebRtcStreamService.CreateAnswerAsync(offer, cancellationToken);
            return Results.Json(answer);
        });

        app.MapGet("/mjpeg", async (HttpContext ctx) =>
        {
            var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
            if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = AppText.Get("Program_Security_MissingCode_Error") });
                return;
            }

            if (!runtime.ViewerLimiter.TryEnterMjpeg())
            {
                ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.Response.WriteAsJsonAsync(new { error = AppText.Get("Program_ViewerLimit_Full_Error") });
                return;
            }

            try
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                ctx.Response.Headers.CacheControl = "no-store, no-cache";
                ctx.Response.Headers.Pragma = "no-cache";
                ctx.Response.Headers.Connection = "keep-alive";
                ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";

                byte[]? lastFrame = null;
                var token = ctx.RequestAborted;

                while (!token.IsCancellationRequested)
                {
                    var frame = runtime.CaptureService.GetCurrentFrame();
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
                runtime.ViewerLimiter.ExitMjpeg();
            }
        });

        app.MapGet("/cert", () =>
            Results.Bytes(tlsCertDerBytes, "application/x-x509-ca-cert", LocalCertificateProvider.CrtDownloadFileName));

        app.MapGet("/config", (HttpContext ctx) =>
        {
            var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
            if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
                return RuntimeAccessHelper.UnauthorizedResult(runtime);

            return Results.Json(new
            {
                runtime.DisplayName,
                runtime.Config,
                runtime.HostUrl,
                runtime.IpUrl,
            });
        });
    }
}
