using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Web.Services;

internal sealed class CaptureService : ICaptureService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public CaptureService(IRuntimeAccessService runtimeAccess)
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
            return _runtimeAccess.ServiceUnavailableResult();

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
