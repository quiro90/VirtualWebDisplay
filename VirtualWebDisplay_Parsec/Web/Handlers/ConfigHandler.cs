using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Web.Handlers;

internal static class ConfigHandler
{
    internal static IResult HandleConfig(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        if (!RuntimeAccessHelper.TryResolveAuthorizedRuntime(ctx, runtimes, out var runtime, out var runtimeError))
            return runtimeError!;

        return Results.Json(new
        {
            runtime.DisplayName,
            runtime.Config,
            runtime.HostUrl,
            runtime.IpUrl,
        });
    }
}