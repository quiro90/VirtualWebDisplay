using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Maneja la autenticación vía código de seguridad (POST /auth/login).
/// </summary>
internal static class AuthHandler
{
    internal static IResult HandleLogin(
        HttpContext ctx,
        SecurityLoginRequest request,
        IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);

        if (!runtime.SecurityGate.Enabled)
            return Results.Ok(new { authorized = true });

        var result = runtime.SecurityGate.TryAuthorize(ctx, RuntimeAccessHelper.SecurityCookieName(runtime), request.Code);

        if (result.Authorized)
            return Results.Ok(new { authorized = true });

        if (result.TooManyAttempts)
        {
            return Results.Json(
                new
                {
                    error             = AppText.Format("Program_Security_TooManyAttempts_Error", result.RetryAfterSeconds),
                    retryAfterSeconds = result.RetryAfterSeconds,
                    attemptsRemaining = 0,
                },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        return Results.Json(
            new
            {
                error             = AppText.Get("Program_Security_InvalidCode_Error"),
                attemptsRemaining = result.AttemptsRemaining,
            },
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
