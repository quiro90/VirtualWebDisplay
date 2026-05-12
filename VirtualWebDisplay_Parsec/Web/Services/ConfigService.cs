using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Web.Services;

internal sealed class ConfigService : IConfigService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public ConfigService(IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleConfig(HttpContext ctx)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out var runtime, out var runtimeError))
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
