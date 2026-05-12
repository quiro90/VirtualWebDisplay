using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Web.Api;
using VirtualWebDisplay.Web.Handlers;
using VirtualWebDisplay.Web.Services;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class InputHandlerTests
{
    [Fact]
    public async Task HandleTouchInput_ReturnsBadRequest_WhenTypeIsMissing()
    {
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = new TouchInputRequest { Type = string.Empty };

        var handler = new TouchInputHandler(new RuntimeAccessService(Array.Empty<ScreenRuntimeContext>()));

        var result = handler.HandleTouchInput(context, request);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Contains("Type field required", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleTouchInput_ReturnsNoContent_WhenTouchInputIsDisabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(configure: config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = CreateTouchRequest(type: "touchmove", action: "tap", x: 10, y: 20);

        var handler = CreateTouchInputHandler(runtime);

        var result = handler.HandleTouchInput(context, request);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status204NoContent, response.StatusCode);
    }

    [Fact]
    public async Task HandleTouchInput_ReturnsBadRequest_WhenCoordinatesMissingForNonTerminalAction()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(configure: config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = new TouchInputRequest
        {
            Type = "touchmove",
            Action = "tap",
            X = null,
            Y = null,
            ViewportWidth = 100,
            ViewportHeight = 200,
        };

        var handler = CreateTouchInputHandler(runtime);

        var result = handler.HandleTouchInput(context, request);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Contains("Coordinates X and Y are required", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleTouchInput_ReturnsOk_WhenDragEndHasNoCoordinates()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(configure: config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = true;
            config.TouchPreserveCursor = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = new TouchInputRequest
        {
            Type = "touchend",
            Action = "dragend",
            X = null,
            Y = null,
        };

        var handler = CreateTouchInputHandler(runtime);

        var result = handler.HandleTouchInput(context, request);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
    }

    [Fact]
    public async Task HandleTouchInput_ReturnsNoContent_WhenScrollActionIsDisabled()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(configure: config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = true;
            config.TouchScrollEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = CreateTouchRequest(type: "touchmove", action: "scrollmove", x: 10, y: 20, fingers: 2);
        request.ScrollDeltaX = 3;
        request.ScrollDeltaY = 4;

        var handler = CreateTouchInputHandler(runtime);

        var result = handler.HandleTouchInput(context, request);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status204NoContent, response.StatusCode);
    }

    [Fact]
    public async Task HandleTouchInput_ReturnsBadRequest_ForUnknownAction()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(configure: config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = CreateTouchRequest(type: "touchmove", action: "mystery-action", x: 10, y: 20);

        var handler = CreateTouchInputHandler(runtime);

        var result = handler.HandleTouchInput(context, request);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Contains("Unknown action", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleTouchInput_ReturnsBadRequest_ForUnknownLegacyType()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(configure: config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = true;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = CreateTouchRequest(type: "legacy-unknown", action: string.Empty, x: 10, y: 20);

        var handler = CreateTouchInputHandler(runtime);

        var result = handler.HandleTouchInput(context, request);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Contains("Unknown legacy type", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InputService_PreservesTelemetryBetweenRequests()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.TouchInputEnabled = true;
        });
        var service = new InputService(new RuntimeAccessService([runtime]));

        var inputContext = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var request = CreateTouchRequest(type: "touchmove", action: "mystery-action", x: 10, y: 20);
        await WebHandlerTestHelper.ExecuteResultAsync(service.HandleTouchInput(inputContext, request), inputContext);

        var statsContext = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(service.HandleTouchStats(statsContext), statsContext);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Contains("\"totalEvents\":1", response.Body, StringComparison.Ordinal);
        Assert.Contains("\"totalErrors\":1", response.Body, StringComparison.Ordinal);
    }

    private static TouchInputRequest CreateTouchRequest(string type, string action, double x, double y, int fingers = 1) => new()
    {
        Type = type,
        Action = action,
        X = x,
        Y = y,
        ViewportWidth = 100,
        ViewportHeight = 200,
        Fingers = fingers,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };

    private static TouchInputHandler CreateTouchInputHandler(ScreenRuntimeContext runtime) =>
        new(new RuntimeAccessService([runtime]));
}
