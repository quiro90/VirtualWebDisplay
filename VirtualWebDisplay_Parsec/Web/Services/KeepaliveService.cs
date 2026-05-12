using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Web.Services;

internal sealed class KeepaliveService : IKeepaliveService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public KeepaliveService(IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleKeepalive(HttpContext ctx)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out _, out var runtimeError))
            return runtimeError!;

        return Results.NoContent();
    }
}
