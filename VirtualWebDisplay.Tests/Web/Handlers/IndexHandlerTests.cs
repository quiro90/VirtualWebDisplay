using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Web.Handlers;
using VirtualWebDisplay.Web.HtmlTemplates;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class IndexHandlerTests
{
    [Fact]
    public async Task HandleIndex_ReturnsSecurityPage_WhenNotAuthorized()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = true;
            c.MaxViewers = 0;
            c.TransmissionMethod = TransmissionModeOptions.WebImage;
        });

        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = IndexHandler.HandleIndex(
            context,
            [runtime],
            new WebImagePageTemplate(),
            new RtcPageTemplate(),
            new SecurityPageTemplate(),
            new ViewerLimitPageTemplate());

        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("authForm", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleIndex_ReturnsViewerLimitPage_WhenCapacityIsFull()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = false;
            c.MaxViewers = 1;
            c.TransmissionMethod = TransmissionModeOptions.Rtc;
        });
        runtime.ViewerLimiter.GetWebRtcCount = () => 1;

        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = IndexHandler.HandleIndex(
            context,
            [runtime],
            new WebImagePageTemplate(),
            new RtcPageTemplate(),
            new SecurityPageTemplate(),
            new ViewerLimitPageTemplate());

        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("128683", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleIndex_ReturnsWebImagePage_WhenAuthorizedAndModeIsWebImage()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = false;
            c.MaxViewers = 0;
            c.TransmissionMethod = TransmissionModeOptions.WebImage;
        });

        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = IndexHandler.HandleIndex(
            context,
            [runtime],
            new WebImagePageTemplate(),
            new RtcPageTemplate(),
            new SecurityPageTemplate(),
            new ViewerLimitPageTemplate());

        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("WebImageClient", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleIndex_ReturnsRtcPage_WhenAuthorizedAndModeIsRtc()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(c =>
        {
            c.Port = 8000;
            c.ScreenSecurityEnabled = false;
            c.MaxViewers = 0;
            c.TransmissionMethod = TransmissionModeOptions.Rtc;
        });

        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = IndexHandler.HandleIndex(
            context,
            [runtime],
            new WebImagePageTemplate(),
            new RtcPageTemplate(),
            new SecurityPageTemplate(),
            new ViewerLimitPageTemplate());

        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("WebRTC H.264", response.Body, StringComparison.Ordinal);
    }
}
