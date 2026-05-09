using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Web.Api;
using VirtualWebDisplay.Web.Handlers;

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

        var result = AuthHandler.HandleLogin(context, new SecurityLoginRequest("ignored"), [runtime]);
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

        var result = AuthHandler.HandleLogin(context, new SecurityLoginRequest("BAD999"), [runtime]);
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

        var result = AuthHandler.HandleLogin(context, new SecurityLoginRequest(runtime.SecurityGate.AccessCode), [runtime]);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("authorized", response.Body, StringComparison.Ordinal);
        Assert.True(response.Headers.ContainsKey("Set-Cookie"));
    }
}
