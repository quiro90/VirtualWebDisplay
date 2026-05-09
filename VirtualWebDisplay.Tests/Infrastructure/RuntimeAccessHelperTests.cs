using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Tests.Web.Handlers;

namespace VirtualWebDisplay.Tests.Infrastructure;

public sealed class RuntimeAccessHelperTests
{
    [Theory]
    [InlineData("contain", "contain")]
    [InlineData(" fill ", "fill")]
    [InlineData("COVER", "cover")]
    [InlineData("", "cover")]
    [InlineData(null, "cover")]
    [InlineData("unknown", "cover")]
    public void NormalizeBrowserImageFit_NormalizesExpectedValues(string? input, string expected)
    {
        var result = RuntimeAccessHelper.NormalizeBrowserImageFit(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveRuntime_ReturnsHttpPortMatch()
    {
        using var runtime1 = WebHandlerTestHelper.CreateRuntime(c => c.Port = 8000, id: "screen1");
        using var runtime2 = WebHandlerTestHelper.CreateRuntime(c => c.Port = 8010, id: "screen2");
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8010);

        var resolved = RuntimeAccessHelper.ResolveRuntime(context, [runtime1, runtime2]);

        Assert.Same(runtime2, resolved);
    }

    [Fact]
    public void ResolveRuntime_ReturnsHttpsPortMatch()
    {
        using var runtime1 = WebHandlerTestHelper.CreateRuntime(c => c.Port = 8000, id: "screen1");
        using var runtime2 = WebHandlerTestHelper.CreateRuntime(c => c.Port = 8010, id: "screen2");
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8001);

        var resolved = RuntimeAccessHelper.ResolveRuntime(context, [runtime1, runtime2]);

        Assert.Same(runtime1, resolved);
    }

    [Fact]
    public void ResolveRuntime_FallsBackToFirstRuntime_WhenNoPortMatches()
    {
        using var runtime1 = WebHandlerTestHelper.CreateRuntime(c => c.Port = 8000, id: "screen1");
        using var runtime2 = WebHandlerTestHelper.CreateRuntime(c => c.Port = 8010, id: "screen2");
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 9999);

        var resolved = RuntimeAccessHelper.ResolveRuntime(context, [runtime1, runtime2]);

        Assert.Same(runtime1, resolved);
    }

    [Fact]
    public void ResolveViewerKey_ReturnsCookieSession_WhenSecurityEnabledAndCookieExists()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var cookieName = RuntimeAccessHelper.SecurityCookieName(runtime);
        context.Request.Headers.Cookie = $"{cookieName}=session-abc";

        var viewerKey = RuntimeAccessHelper.ResolveViewerKey(context, runtime);

        Assert.Equal("session-abc", viewerKey);
    }

    [Fact]
    public void ResolveViewerKey_ReturnsRemoteIp_WhenCookieMissingOrSecurityDisabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var viewerKey = RuntimeAccessHelper.ResolveViewerKey(context, runtime);

        Assert.Equal("127.0.0.1", viewerKey);
    }

    [Fact]
    public void ResolveViewerKey_ReturnsUnknown_WhenRemoteIpIsNull()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        context.Connection.RemoteIpAddress = null;

        var viewerKey = RuntimeAccessHelper.ResolveViewerKey(context, runtime);

        Assert.Equal("unknown", viewerKey);
    }

    [Fact]
    public async Task UnauthorizedResult_ReturnsJson401_WhenSecurityEnabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = RuntimeAccessHelper.UnauthorizedResult(runtime);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
        Assert.Contains("error", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnauthorizedResult_ReturnsUnauthorized401_WhenSecurityDisabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = RuntimeAccessHelper.UnauthorizedResult(runtime);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
    }

    [Fact]
    public void TryResolveAuthorizedRuntime_ReturnsTrueAndRuntime_WhenAuthorized()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var ok = RuntimeAccessHelper.TryResolveAuthorizedRuntime(context, [runtime], out var resolved, out var unauthorized);

        Assert.True(ok);
        Assert.Same(runtime, resolved);
        Assert.Null(unauthorized);
    }

    [Fact]
    public void TryResolveAuthorizedRuntime_ReturnsFalseAndUnauthorized_WhenNotAuthorized()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var ok = RuntimeAccessHelper.TryResolveAuthorizedRuntime(context, [runtime], out var resolved, out var unauthorized);

        Assert.False(ok);
        Assert.Same(runtime, resolved);
        Assert.NotNull(unauthorized);
    }

    [Fact]
    public async Task NotFoundResult_Returns404()
    {
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var response = await WebHandlerTestHelper.ExecuteResultAsync(RuntimeAccessHelper.NotFoundResult(), context);

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TooManyRequestsResult_Returns429()
    {
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var response = await WebHandlerTestHelper.ExecuteResultAsync(RuntimeAccessHelper.TooManyRequestsResult(), context);

        Assert.Equal(StatusCodes.Status429TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task InternalServerErrorResult_Returns500()
    {
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var response = await WebHandlerTestHelper.ExecuteResultAsync(RuntimeAccessHelper.InternalServerErrorResult(), context);

        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task ServiceUnavailableResult_Returns503()
    {
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var response = await WebHandlerTestHelper.ExecuteResultAsync(RuntimeAccessHelper.ServiceUnavailableResult(), context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task HtmlContent_ReturnsTextHtmlBody()
    {
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var response = await WebHandlerTestHelper.ExecuteResultAsync(RuntimeAccessHelper.HtmlContent("<h1>ok</h1>"), context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("<h1>ok</h1>", response.Body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValue("Content-Type", out var contentType));
        Assert.Contains("text/html", contentType.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
