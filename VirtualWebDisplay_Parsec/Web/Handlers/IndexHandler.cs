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
                return RuntimeAccessHelper.HtmlContent(viewerLimitPageTemplate.Generate(runtime));
        }

        if (!isAuthorized)
            return RuntimeAccessHelper.HtmlContent(securityPageTemplate.Generate(runtime, ctx));

        var parameters = BuildTemplateParameters(runtime);
        var html = GenerateDisplayPage(runtime, parameters, webImageTemplate, rtcTemplate);

        return RuntimeAccessHelper.HtmlContent(html);
    }

    private static Dictionary<string, object> BuildTemplateParameters(ScreenRuntimeContext runtime)
    {
        var browserImageFit = RuntimeAccessHelper.NormalizeBrowserImageFit(runtime.Config.BrowserImageFit);
        return new Dictionary<string, object>
        {
            ["title"] = runtime.DisplayName,
            ["browserImageFit"] = browserImageFit,
            ["intervalMs"] = Math.Max(3, (int)Math.Round(runtime.Config.CaptureIntervalSeconds * 1000)),
            ["capToken"] = runtime.CapToken,
            ["touchZoomEnabled"] = runtime.Config.TouchZoomEnabled,
            ["touchZoomDelayMs"] = runtime.Config.TouchZoomDelayMs,
            ["touchHoldEnabled"] = runtime.Config.TouchHoldEnabled,
            ["touchHoldDelayMs"] = runtime.Config.TouchHoldDelayMs,
            ["touchScrollEnabled"] = runtime.Config.TouchScrollEnabled,
            ["touchScrollDelayMs"] = runtime.Config.TouchScrollDelayMs,
        };
    }

    private static string GenerateDisplayPage(
        ScreenRuntimeContext runtime,
        Dictionary<string, object> parameters,
        WebImagePageTemplate webImageTemplate,
        RtcPageTemplate rtcTemplate) =>
        TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod)
            ? webImageTemplate.Generate(parameters)
            : rtcTemplate.Generate(parameters);
}
