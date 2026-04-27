using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.UI.HtmlTemplates;

namespace VirtualWebDisplay.Controllers.Handlers;

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
            if (TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod))
            {
                var canContinue = isAuthorized
                    ? runtime.ViewerLimiter.TryRegisterPolling(RuntimeAccessHelper.ResolveViewerKey(ctx, runtime))
                    : runtime.ViewerLimiter.CanAcceptViewer();

                if (!canContinue)
                    return Results.Content(viewerLimitPageTemplate.Generate(runtime), "text/html");
            }
            else
            {
                if (!runtime.ViewerLimiter.CanAcceptViewer())
                    return Results.Content(viewerLimitPageTemplate.Generate(runtime), "text/html");
            }
        }

        if (!isAuthorized)
            return Results.Content(securityPageTemplate.Generate(runtime, ctx), "text/html");

        var browserImageFit = RuntimeAccessHelper.NormalizeBrowserImageFit(runtime.Config.BrowserImageFit);

        string html;
        if (TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod))
        {
            var parameters = new Dictionary<string, object>
            {
                ["title"]           = runtime.DisplayName,
                ["browserImageFit"] = browserImageFit,
                ["intervalMs"]      = Math.Max(3, (int)Math.Round(runtime.Config.CaptureIntervalSeconds * 1000)),
                ["touchInputEnabled"] = runtime.Config.TouchInputEnabled,
            };
            html = webImageTemplate.Generate(parameters);
        }
        else
        {
            var parameters = new Dictionary<string, object>
            {
                ["title"]           = runtime.DisplayName,
                ["browserImageFit"] = browserImageFit,
                ["touchInputEnabled"] = runtime.Config.TouchInputEnabled,
            };
            html = rtcTemplate.Generate(parameters);
        }

        return Results.Content(html, "text/html");
    }
}
