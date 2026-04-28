using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Controllers.Handlers;

internal static class ConfigHandler
{
    internal static IResult HandleConfig(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        return Results.Json(new
        {
            runtime.DisplayName,
            runtime.Config,
            runtime.HostUrl,
            runtime.IpUrl,
        });
    }
}