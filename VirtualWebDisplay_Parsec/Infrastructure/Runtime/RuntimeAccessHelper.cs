using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Infrastructure.Runtime;

public static class RuntimeAccessHelper
{
    public static string NormalizeBrowserImageFit(string? fit) =>
        fit?.Trim().ToLowerInvariant() switch
        {
            "contain" => "contain",
            "fill" => "fill",
            _ => "cover",
        };

    public static string SecurityCookieName(ScreenRuntimeContext runtime) => $"vwd_auth_{runtime.Id}";

    public static ScreenRuntimeContext ResolveRuntime(HttpContext context, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        var localPort = context.Connection.LocalPort;

        // Intentar match directo con puerto HTTP
        var runtime = runtimes.FirstOrDefault(r => r.Config.Port == localPort);
        if (runtime != null)
            return runtime;

        // Intentar match con puerto HTTPS (Config.Port + 1)
        runtime = runtimes.FirstOrDefault(r => r.Config.Port + 1 == localPort);
        if (runtime != null)
            return runtime;

        // Fallback a primera pantalla
        return runtimes[0];
    }

    public static bool IsAuthorized(HttpContext context, ScreenRuntimeContext runtime) =>
        !runtime.SecurityGate.Enabled || runtime.SecurityGate.IsAuthorized(context, SecurityCookieName(runtime));

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
}
