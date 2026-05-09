using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class WebRtcHandlerTests
{
    [Fact]
    public async Task HandleOffer_ReturnsBadRequest_WhenTransmissionModeIsNotRtc()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.TransmissionMethod = TransmissionModeOptions.WebImage;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = await WebRtcHandler.HandleOffer(
            context,
            new WebRtcSessionOffer("fake-sdp", "offer"),
            [runtime],
            CancellationToken.None);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HandleOffer_ReturnsTooManyRequests_WhenViewerLimitIsFull()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.TransmissionMethod = TransmissionModeOptions.Rtc;
            config.MaxViewers = 1;
        });
        runtime.ViewerLimiter.GetWebRtcCount = () => 1;
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = await WebRtcHandler.HandleOffer(
            context,
            new WebRtcSessionOffer("fake-sdp", "offer"),
            [runtime],
            CancellationToken.None);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status429TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task HandleOffer_ReturnsBadRequest_WhenOfferPayloadIsInvalid()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.TransmissionMethod = TransmissionModeOptions.Rtc;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = await WebRtcHandler.HandleOffer(
            context,
            new WebRtcSessionOffer("", "answer"),
            [runtime],
            CancellationToken.None);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
    }
}
