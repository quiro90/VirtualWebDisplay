using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Infrastructure.Runtime;

public static class RuntimeAccessHelper
{
    private const int TooManyRequestsStatusCode = StatusCodes.Status429TooManyRequests;
    private const int ViewerLimitExceededStatusCode = StatusCodes.Status429TooManyRequests;
    private const int InternalServerErrorStatusCode = StatusCodes.Status500InternalServerError;
    private const int ServiceUnavailableStatusCode = StatusCodes.Status503ServiceUnavailable;

    private const string BrowserFitContain = "contain";
    private const string BrowserFitFill = "fill";
    private const string BrowserFitCover = "cover";

    public static string NormalizeBrowserImageFit(string? fit) =>
        fit?.Trim().ToLowerInvariant() switch
        {
            BrowserFitContain => BrowserFitContain,
            BrowserFitFill => BrowserFitFill,
            _ => BrowserFitCover,
        };

    public static string SecurityCookieName(ScreenRuntimeContext runtime) => $"vwd_auth_{runtime.Id}";

    public static ScreenRuntimeContext ResolveRuntime(HttpContext context, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        var localPort = context.Connection.LocalPort;

        if (TryResolveRuntimeByPort(localPort, runtimes, out var runtime))
            return runtime;

        // Fallback a primera pantalla
        return runtimes[0];
    }

    private static bool TryResolveRuntimeByPort(
        int localPort,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        out ScreenRuntimeContext runtime)
    {
        var matchedRuntime = runtimes.FirstOrDefault(r => MatchesRuntimePort(localPort, r));
        if (matchedRuntime is null)
        {
            runtime = runtimes[0];
            return false;
        }

        runtime = matchedRuntime;
        return true;
    }

    private static bool MatchesRuntimePort(int localPort, ScreenRuntimeContext runtime) =>
        runtime.Config.Port == localPort || runtime.Config.Port + 1 == localPort;

    public static bool IsAuthorized(HttpContext context, ScreenRuntimeContext runtime) =>
        !runtime.SecurityGate.Enabled || runtime.SecurityGate.IsAuthorized(context, SecurityCookieName(runtime));

    public static bool TryResolveAuthorizedRuntime(
        HttpContext context,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        out ScreenRuntimeContext runtime,
        out IResult? unauthorizedResult)
    {
        runtime = ResolveRuntime(context, runtimes);
        if (IsAuthorized(context, runtime))
        {
            unauthorizedResult = null;
            return true;
        }

        unauthorizedResult = UnauthorizedResult(runtime);
        return false;
    }

    public static string ResolveViewerKey(HttpContext context, ScreenRuntimeContext runtime)
    {
        var cookieName = SecurityCookieName(runtime);
        if (runtime.SecurityGate.Enabled
            && context.Request.Cookies.TryGetValue(cookieName, out var sessionId)
            && !string.IsNullOrWhiteSpace(sessionId))
            return sessionId;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static IResult UnauthorizedResult(ScreenRuntimeContext runtime)
    {
        if (runtime.SecurityGate.Enabled)
        {
            return Results.Json(
                new { error = AppText.Get("Program_Security_MissingCode_Error") },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Unauthorized();
    }

    public static IResult BadRequestError(string message) =>
        Results.BadRequest(new { error = message });

    public static IResult AuthorizedResult() =>
        Results.Ok(new { authorized = true });

    public static IResult NotFoundResult() =>
        Results.NotFound();

    public static IResult TooManyRequestsResult() =>
        Results.StatusCode(TooManyRequestsStatusCode);

    public static IResult InternalServerErrorResult() =>
        Results.StatusCode(InternalServerErrorStatusCode);

    public static IResult ServiceUnavailableResult() =>
        Results.StatusCode(ServiceUnavailableStatusCode);

    public static IResult HtmlContent(string html) =>
        Results.Content(html, "text/html");

    public static IResult ViewerLimitExceededResult() =>
        Results.Json(
            new { error = AppText.Get("Program_ViewerLimit_Full_Error") },
            statusCode: ViewerLimitExceededStatusCode);

    public static Task WriteViewerLimitExceededAsync(HttpContext context) =>
        ViewerLimitExceededResult().ExecuteAsync(context);
}
