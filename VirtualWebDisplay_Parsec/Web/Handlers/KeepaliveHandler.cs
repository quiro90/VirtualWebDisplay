using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Web.Handlers;

internal static class KeepaliveHandler
{
    internal static IResult HandleKeepalive(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        if (!RuntimeAccessHelper.TryResolveAuthorizedRuntime(ctx, runtimes, out _, out var runtimeError))
            return runtimeError!;

        return Results.NoContent();
    }
}