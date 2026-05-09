using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class CaptureHandlerTests
{
    [Fact]
    public async Task HandleCapture_ReturnsNotFound_WhenTokenDoesNotMatch()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = CaptureHandler.HandleCapture(context, token: "invalid-token", [runtime]);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HandleCapture_ReturnsUnauthorized_WhenSecurityEnabledAndNoSession()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = CaptureHandler.HandleCapture(context, runtime.CapToken, [runtime]);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HandleCapture_ReturnsTooManyRequests_WhenViewerLimitIsFull()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
            config.MaxViewers = 1;
        });
        runtime.ViewerLimiter.GetWebRtcCount = () => 1;
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = CaptureHandler.HandleCapture(context, runtime.CapToken, [runtime]);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status429TooManyRequests, response.StatusCode);
        Assert.Contains("error", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMjpeg_ReturnsUnauthorized_WhenSecurityEnabledAndNoSession()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        await CaptureHandler.HandleMjpeg(context, [runtime]);

        var response = await ReadHttpResponseAsync(context);
        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
        Assert.Contains("error", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMjpeg_ReturnsTooManyRequests_WhenViewerLimitIsFull()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
            config.MaxViewers = 1;
        });
        runtime.ViewerLimiter.GetWebRtcCount = () => 1;
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        await CaptureHandler.HandleMjpeg(context, [runtime]);

        var response = await ReadHttpResponseAsync(context);
        Assert.Equal(StatusCodes.Status429TooManyRequests, response.StatusCode);
        Assert.Contains("error", response.Body, StringComparison.Ordinal);
    }

    private static async Task<(int StatusCode, string Body)> ReadHttpResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }
}
