using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Maneja la página principal del display (GET /).
/// Sirve la UI de WebImage o WebRTC según la configuración del runtime.
/// </summary>
internal static class IndexHandler
{
    internal static IResult HandleIndex(
        HttpContext ctx,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        WebImagePageTemplate webImageTemplate,
        RtcPageTemplate rtcTemplate,
        SecurityPageTemplate securityPageTemplate,
        ViewerLimitPageTemplate viewerLimitPageTemplate)
    {
        var runtime      = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
        var isAuthorized = RuntimeAccessHelper.IsAuthorized(ctx, runtime);

        if (!runtime.ViewerLimiter.IsUnlimited)
        {
            var isWebImage = TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod);
            var canContinue = isWebImage && isAuthorized
                ? runtime.ViewerLimiter.TryRegisterPolling(RuntimeAccessHelper.ResolveViewerKey(ctx, runtime))
                : runtime.ViewerLimiter.CanAcceptViewer();

            if (!canContinue)
                return Results.Content(viewerLimitPageTemplate.Generate(runtime), "text/html");
        }

        if (!isAuthorized)
            return Results.Content(securityPageTemplate.Generate(runtime, ctx), "text/html");

        var browserImageFit = RuntimeAccessHelper.NormalizeBrowserImageFit(runtime.Config.BrowserImageFit);
        var parameters = new Dictionary<string, object>
        {
            ["title"] = runtime.DisplayName,
            ["browserImageFit"] = browserImageFit,
            ["intervalMs"] = Math.Max(3, (int)Math.Round(runtime.Config.CaptureIntervalSeconds * 1000)),
            ["touchZoomEnabled"] = runtime.Config.TouchZoomEnabled,
            ["touchZoomDelayMs"] = runtime.Config.TouchZoomDelayMs,
            ["touchHoldEnabled"] = runtime.Config.TouchHoldEnabled,
            ["touchHoldDelayMs"] = runtime.Config.TouchHoldDelayMs,
            ["touchScrollEnabled"] = runtime.Config.TouchScrollEnabled,
            ["touchScrollDelayMs"] = runtime.Config.TouchScrollDelayMs,
        };

        var html = TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod)
            ? webImageTemplate.Generate(parameters)
            : rtcTemplate.Generate(parameters);

        return Results.Content(html, "text/html");
    }
}
