using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Web.Api;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class HandlerConcurrencyTests
{
    [Fact]
    public async Task HandleTouchInput_ConcurrentRequests_WhenTouchDisabled_AllReturnNoContent()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = false;
            config.ScreenSecurityEnabled = false;
        });

        const int requests = 80;
        var tasks = Enumerable.Range(0, requests).Select(async _ =>
        {
            var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
            var request = new TouchInputRequest
            {
                Type = "touchmove",
                Action = "tap",
                X = 50,
                Y = 50,
                ViewportWidth = 100,
                ViewportHeight = 100,
            };

            var result = InputHandler.HandleTouchInput(context, request, [runtime]);
            var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);
            return response.StatusCode;
        });

        var statuses = await Task.WhenAll(tasks);

        Assert.All(statuses, status => Assert.Equal(StatusCodes.Status204NoContent, status));
    }

    [Fact]
    public async Task HandleKeepalive_ConcurrentRequests_AllReturnNoContent()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });

        const int requests = 100;
        var tasks = Enumerable.Range(0, requests).Select(async _ =>
        {
            var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
            var result = KeepaliveHandler.HandleKeepalive(context, [runtime]);
            var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);
            return response.StatusCode;
        });

        var statuses = await Task.WhenAll(tasks);

        Assert.All(statuses, status => Assert.Equal(StatusCodes.Status204NoContent, status));
    }

    [Fact]
    public async Task HandleConfig_ConcurrentAuthorizedRequests_AllReturnOk()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });

        const int requests = 80;
        var tasks = Enumerable.Range(0, requests).Select(async _ =>
        {
            var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
            var result = ConfigHandler.HandleConfig(context, [runtime]);
            var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);
            return (response.StatusCode, response.Body);
        });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, response =>
        {
            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.Contains("hostUrl", response.Body, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task HandleLogin_ConcurrentValidRequests_AllReturnAuthorized()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });

        const int requests = 60;
        var tasks = Enumerable.Range(0, requests).Select(async _ =>
        {
            var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000, isHttps: true);
            var result = AuthHandler.HandleLogin(context, new SecurityLoginRequest(runtime.SecurityGate.AccessCode), [runtime]);
            var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);
            return response;
        });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, response =>
        {
            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.Contains("authorized", response.Body, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task HandleLogin_ConcurrentInvalidRequests_ReturnUnauthorizedOrThrottle()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });

        const int requests = 40;
        var tasks = Enumerable.Range(0, requests).Select(async _ =>
        {
            var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
            var result = AuthHandler.HandleLogin(context, new SecurityLoginRequest("BAD999"), [runtime]);
            var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);
            return response.StatusCode;
        });

        var statuses = await Task.WhenAll(tasks);

        Assert.All(statuses, status =>
            Assert.True(
                status is StatusCodes.Status401Unauthorized or StatusCodes.Status429TooManyRequests,
                $"Unexpected status {status}"));
    }
}
