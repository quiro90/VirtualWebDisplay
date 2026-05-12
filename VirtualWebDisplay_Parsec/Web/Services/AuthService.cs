using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Web.Services;

internal sealed class AuthService : IAuthService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public AuthService(IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public IResult HandleLogin(HttpContext ctx, SecurityLoginRequest request)
    {
        var runtime = _runtimeAccess.ResolveRuntime(ctx);

        if (!runtime.SecurityGate.Enabled)
            return _runtimeAccess.AuthorizedResult();

        var result = runtime.SecurityGate.TryAuthorize(ctx, _runtimeAccess.SecurityCookieName(runtime), request.Code);

        if (result.Authorized)
            return _runtimeAccess.AuthorizedResult();

        if (result.TooManyAttempts)
        {
            return Results.Json(
                new
                {
                    error = AppText.Format("Program_Security_TooManyAttempts_Error", result.RetryAfterSeconds),
                    retryAfterSeconds = result.RetryAfterSeconds,
                    attemptsRemaining = 0,
                },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        return Results.Json(
            new
            {
                error = AppText.Get("Program_Security_InvalidCode_Error"),
                attemptsRemaining = result.AttemptsRemaining,
            },
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
