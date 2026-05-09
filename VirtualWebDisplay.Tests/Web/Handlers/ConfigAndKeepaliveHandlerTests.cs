using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class ConfigAndKeepaliveHandlerTests
{
    [Fact]
    public async Task HandleConfig_ReturnsUnauthorized_WhenSecurityEnabledAndNoSession()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = ConfigHandler.HandleConfig(context, [runtime]);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HandleConfig_ReturnsRuntimeData_WhenAuthorizedByDisabledSecurity()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = ConfigHandler.HandleConfig(context, [runtime]);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("Screen 1", response.Body, StringComparison.Ordinal);
        Assert.Contains("hostUrl", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleKeepalive_ReturnsNoContent_WhenAuthorizedByDisabledSecurity()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = KeepaliveHandler.HandleKeepalive(context, [runtime]);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status204NoContent, response.StatusCode);
    }
}
