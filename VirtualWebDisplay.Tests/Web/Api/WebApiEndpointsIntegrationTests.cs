using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Web.Api;
using VirtualWebDisplay.Web.Handlers;
using VirtualWebDisplay.Tests.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Api;

public sealed class WebApiEndpointsIntegrationTests
{
    [Fact]
    public async Task KeepaliveEndpoint_ReturnsNoContent_WhenSecurityDisabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });

        await using var app = await BuildTestAppAsync(runtime);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/keepalive");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ConfigEndpoint_ReturnsUnauthorized_WhenSecurityEnabledAndNoCookie()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });

        await using var app = await BuildTestAppAsync(runtime);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/config");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("error", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthLoginEndpoint_ReturnsUnauthorized_WhenCodeInvalid()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });

        await using var app = await BuildTestAppAsync(runtime);
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/auth/login", new SecurityLoginRequest("BAD999"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("attemptsRemaining", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthThenConfig_WithReturnedCookie_ReturnsAuthorizedConfig()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = true;
        });

        await using var app = await BuildTestAppAsync(runtime);
        var client = app.GetTestClient();

        var login = await client.PostAsJsonAsync("/auth/login", new SecurityLoginRequest(runtime.SecurityGate.AccessCode));
        var loginBody = await login.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("authorized", loginBody, StringComparison.Ordinal);
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var setCookies));

        var cookieHeader = setCookies!.First().Split(';', StringSplitOptions.RemoveEmptyEntries)[0];
        var configRequest = new HttpRequestMessage(HttpMethod.Get, "/config");
        configRequest.Headers.Add("Cookie", cookieHeader);

        var config = await client.SendAsync(configRequest);
        var configBody = await config.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, config.StatusCode);
        Assert.Contains("hostUrl", configBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InputTouchEndpoint_ReturnsNoContent_WhenTouchDisabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
            config.TouchInputEnabled = false;
        });

        await using var app = await BuildTestAppAsync(runtime);
        var client = app.GetTestClient();

        var request = new TouchInputRequest
        {
            Type = "touchmove",
            Action = "tap",
            X = 10,
            Y = 10,
            ViewportWidth = 100,
            ViewportHeight = 100,
        };

        var response = await client.PostAsJsonAsync("/input/touch", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<WebApplication> BuildTestAppAsync(ScreenRuntimeContext runtime)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var orchestrator = new DefaultWebEndpointOrchestrator([runtime], [1, 2, 3]);
        builder.Services.AddSingleton<IWebEndpointOrchestrator>(orchestrator);

        var app = builder.Build();
        WebApiEndpoints.Map(app);
        await app.StartAsync();
        return app;
    }
}
