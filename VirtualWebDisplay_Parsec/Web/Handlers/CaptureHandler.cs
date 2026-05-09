using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Maneja la captura de pantalla como imagen estática (GET /cap)
/// y como stream MJPEG continuo (GET /mjpeg).
/// </summary>
internal static class CaptureHandler
{
    internal static IResult HandleCapture(HttpContext ctx, string token, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);

        if (!string.Equals(token, runtime.CapToken, StringComparison.Ordinal))
            return RuntimeAccessHelper.NotFoundResult();

        if (!RuntimeAccessHelper.TryResolveAuthorizedRuntime(ctx, runtimes, out runtime, out var runtimeError))
            return runtimeError!;

        if (!runtime.ViewerLimiter.IsUnlimited)
        {
            var viewerKey = RuntimeAccessHelper.ResolveViewerKey(ctx, runtime);
            if (!runtime.ViewerLimiter.TryRegisterPolling(viewerKey))
                return RuntimeAccessHelper.ViewerLimitExceededResult();
        }

        runtime.FrameSource.NotifyJpegDemand();
        var frame = runtime.FrameSource.GetCurrentJpegFrame();
        if (frame.Length == 0)
            return RuntimeAccessHelper.ServiceUnavailableResult();

        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        return Results.Bytes(frame, "image/jpeg");
    }

    internal static async Task HandleMjpeg(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        if (!RuntimeAccessHelper.TryResolveAuthorizedRuntime(ctx, runtimes, out var runtime, out var runtimeError))
        {
            await runtimeError!.ExecuteAsync(ctx);
            return;
        }

        if (!runtime.ViewerLimiter.TryEnterMjpeg())
        {
            await RuntimeAccessHelper.WriteViewerLimitExceededAsync(ctx);
            return;
        }

        runtime.FrameSource.EnterMjpegDemand();

        try
        {
            ctx.Response.StatusCode  = StatusCodes.Status200OK;
            ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
            ctx.Response.Headers.CacheControl = "no-store, no-cache";
            ctx.Response.Headers.Pragma       = "no-cache";
            ctx.Response.Headers.Connection   = "keep-alive";

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
            runtime.FrameSource.ExitMjpegDemand();
            runtime.ViewerLimiter.ExitMjpeg();
        }
    }
}
