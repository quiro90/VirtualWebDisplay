using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Web.Handlers;

internal static class KeepaliveHandler
{
    internal static IResult HandleKeepalive(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        return Results.NoContent();
    }
}