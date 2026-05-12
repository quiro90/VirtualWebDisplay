using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Web.Api;
using VirtualWebDisplay.Web.Services;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class AuthHandlerTests
{
    [Fact]
    public async Task HandleLogin_ReturnsAuthorized_WhenSecurityDisabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = CreateAuthService(runtime).HandleLogin(context, new SecurityLoginRequest("ignored"));
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("authorized", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleLogin_ReturnsUnauthorized_WhenCodeIsInvalid()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = CreateAuthService(runtime).HandleLogin(context, new SecurityLoginRequest("BAD999"));
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
        Assert.Contains("attemptsRemaining", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleLogin_ReturnsAuthorizedAndSetsCookie_WhenCodeIsValid()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000, isHttps: true);

        var result = CreateAuthService(runtime).HandleLogin(context, new SecurityLoginRequest(runtime.SecurityGate.AccessCode));
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("authorized", response.Body, StringComparison.Ordinal);
        Assert.True(response.Headers.ContainsKey("Set-Cookie"));
    }

    private static AuthService CreateAuthService(ScreenRuntimeContext runtime) =>
        new(new RuntimeAccessService([runtime]));
}
