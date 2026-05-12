using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Web.Services;

internal sealed class IndexPageService : IIndexPageService
{
    private readonly IRuntimeAccessService _runtimeAccess;
    private readonly WebImagePageTemplate _webImageTemplate;
    private readonly RtcPageTemplate _rtcTemplate;
    private readonly SecurityPageTemplate _securityPageTemplate;
    private readonly ViewerLimitPageTemplate _viewerLimitPageTemplate;

    public IndexPageService(
        IRuntimeAccessService runtimeAccess,
        WebImagePageTemplate webImageTemplate,
        RtcPageTemplate rtcTemplate,
        SecurityPageTemplate securityPageTemplate,
        ViewerLimitPageTemplate viewerLimitPageTemplate)
    {
        _runtimeAccess = runtimeAccess;
        _webImageTemplate = webImageTemplate;
        _rtcTemplate = rtcTemplate;
        _securityPageTemplate = securityPageTemplate;
        _viewerLimitPageTemplate = viewerLimitPageTemplate;
    }

    public IResult HandleIndex(HttpContext ctx)
    {
        var runtime = _runtimeAccess.ResolveRuntime(ctx);
        var isAuthorized = _runtimeAccess.IsAuthorized(ctx, runtime);

        if (!runtime.ViewerLimiter.IsUnlimited)
        {
            var isWebImage = TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod);
            var canContinue = isWebImage && isAuthorized
                ? runtime.ViewerLimiter.TryRegisterPolling(_runtimeAccess.ResolveViewerKey(ctx, runtime))
                : runtime.ViewerLimiter.CanAcceptViewer();

            if (!canContinue)
                return _runtimeAccess.HtmlContent(_viewerLimitPageTemplate.Generate(runtime));
        }

        if (!isAuthorized)
            return _runtimeAccess.HtmlContent(_securityPageTemplate.Generate(runtime, ctx));

        var parameters = BuildTemplateParameters(runtime);
        var html = GenerateDisplayPage(runtime, parameters);

        return _runtimeAccess.HtmlContent(html);
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

    private string GenerateDisplayPage(ScreenRuntimeContext runtime, Dictionary<string, object> parameters) =>
        TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod)
            ? _webImageTemplate.Generate(parameters)
            : _rtcTemplate.Generate(parameters);
}
