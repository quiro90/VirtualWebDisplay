using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.Streaming;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.Web.Services;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class CaptureHandlerTests
{
    [Fact]
    public async Task HandleCapture_ReturnsJpegBytes_WhenFrameAvailable()
    {
        var frameSource = new FakeFrameCaptureService
        {
            CurrentFrame = [0xFF, 0xD8, 0xAA, 0xFF, 0xD9],
        };
        var factory = new FakeRuntimeServicesFactory(frameSource);

        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        }, servicesFactory: factory);
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = CreateCaptureService(runtime).HandleCapture(context, runtime.CapToken);
        var response = await WebHandlerTestHelper.ExecuteResultAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(1, frameSource.NotifyDemandCalls);
        Assert.Contains("image/jpeg", response.Headers.ContentType.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("no-store, no-cache", response.Headers.CacheControl.ToString());
        Assert.NotEmpty(response.Body);
    }

    [Fact]
    public async Task HandleCapture_ReturnsNotFound_WhenTokenDoesNotMatch()
    {
        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        });
        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);

        var result = CreateCaptureService(runtime).HandleCapture(context, token: "invalid-token");
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

        var result = CreateCaptureService(runtime).HandleCapture(context, runtime.CapToken);
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

        var result = CreateCaptureService(runtime).HandleCapture(context, runtime.CapToken);
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

        await CreateCaptureService(runtime).HandleMjpeg(context);

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

        await CreateCaptureService(runtime).HandleMjpeg(context);

        var response = await ReadHttpResponseAsync(context);
        Assert.Equal(StatusCodes.Status429TooManyRequests, response.StatusCode);
        Assert.Contains("error", response.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMjpeg_StartsStreamAndReleasesDemand_OnCancellation()
    {
        using var cts = new CancellationTokenSource();
        var frameSource = new FakeFrameCaptureService
        {
            CurrentFrame = [0xFF, 0xD8, 0xAB, 0xFF, 0xD9],
            AfterRead = () => cts.Cancel(),
        };
        var factory = new FakeRuntimeServicesFactory(frameSource);

        using var runtime = WebHandlerTestHelper.CreateRuntime(config =>
        {
            config.Port = 8000;
            config.ScreenSecurityEnabled = false;
        }, servicesFactory: factory);

        var context = WebHandlerTestHelper.CreateHttpContext(localPort: 8000);
        context.RequestAborted = cts.Token;

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => CreateCaptureService(runtime).HandleMjpeg(context));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("multipart/x-mixed-replace; boundary=frame", context.Response.ContentType);
        Assert.Equal(1, frameSource.EnterDemandCalls);
        Assert.Equal(1, frameSource.ExitDemandCalls);
    }

    private static async Task<(int StatusCode, string Body)> ReadHttpResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private static CaptureService CreateCaptureService(ScreenRuntimeContext runtime) =>
        new(new RuntimeAccessService([runtime]));

    private sealed class FakeRuntimeServicesFactory : IScreenRuntimeServicesFactory
    {
        private readonly FakeFrameCaptureService _frameSource;

        public FakeRuntimeServicesFactory(FakeFrameCaptureService frameSource)
        {
            _frameSource = frameSource;
        }

        public IFrameCaptureService CreateCaptureService(VirtualScreenConfig config, ILoggerFactory loggerFactory, Func<string?> preferredDeviceNameProvider)
        {
            _ = config;
            _ = loggerFactory;
            _ = preferredDeviceNameProvider;
            return _frameSource;
        }

        public IH264EncoderService CreateH264EncoderService(IFrameCaptureService frameSource, VirtualScreenConfig config, ILoggerFactory loggerFactory)
        {
            _ = frameSource;
            _ = config;
            _ = loggerFactory;
            return new FakeEncoderService();
        }

        public IWebRtcStreamService CreateWebRtcStreamService(IH264EncoderService encoder, ILoggerFactory loggerFactory)
        {
            _ = encoder;
            _ = loggerFactory;
            return new FakeWebRtcService();
        }

        public VirtualDisplayManager CreateDisplayManager(IDriverVerifier driverVerifier) =>
            new(driverVerifier);
    }

    private sealed class FakeFrameCaptureService : IFrameCaptureService
    {
        public byte[] CurrentFrame { get; set; } = [];
        public Action? AfterRead { get; set; }
        public int NotifyDemandCalls { get; private set; }
        public int EnterDemandCalls { get; private set; }
        public int ExitDemandCalls { get; private set; }

        public byte[] GetCurrentJpegFrame()
        {
            var frame = CurrentFrame;
            AfterRead?.Invoke();
            return frame;
        }

        public void NotifyJpegDemand() => NotifyDemandCalls++;
        public void EnterMjpegDemand() => EnterDemandCalls++;
        public void ExitMjpegDemand() => ExitDemandCalls++;

#pragma warning disable CS0067
        public event Action<RawFrame>? RawFrameAvailable;
#pragma warning restore CS0067

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class FakeEncoderService : IH264EncoderService
    {
#pragma warning disable CS0067
        public event Action<byte[], long>? NalUnitReady;
#pragma warning restore CS0067

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class FakeWebRtcService : IWebRtcStreamService
    {
        public int ActivePeerCount => 0;
        public Task<WebRtcSessionAnswer> CreateAnswerAsync(WebRtcSessionOffer offer, CancellationToken cancellationToken) =>
            Task.FromResult(new WebRtcSessionAnswer(string.Empty, "answer", string.Empty));
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() { }
    }
}
