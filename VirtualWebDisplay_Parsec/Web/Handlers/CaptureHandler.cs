using System.Net;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Maneja la captura de pantalla como imagen estática (GET /cap)
/// y como stream MJPEG continuo (GET /mjpeg).
/// </summary>
internal static class CaptureHandler
{
    internal static IResult HandleCapture(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
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
    }

    internal static async Task HandleMjpeg(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
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
            ctx.Response.StatusCode  = (int)HttpStatusCode.OK;
            ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
            ctx.Response.Headers.CacheControl = "no-store, no-cache";
            ctx.Response.Headers.Pragma       = "no-cache";
            ctx.Response.Headers.Connection   = "keep-alive";

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
                await ctx.Response.WriteAsync("--frame\r\n",                             token);
                await ctx.Response.WriteAsync("Content-Type: image/jpeg\r\n",            token);
                await ctx.Response.WriteAsync($"Content-Length: {frame.Length}\r\n\r\n", token);
                await ctx.Response.Body.WriteAsync(frame,                                token);
                await ctx.Response.WriteAsync("\r\n",                                    token);
                await ctx.Response.Body.FlushAsync(token);
            }
        }
        finally
        {
            runtime.ViewerLimiter.ExitMjpeg();
        }
    }
}
