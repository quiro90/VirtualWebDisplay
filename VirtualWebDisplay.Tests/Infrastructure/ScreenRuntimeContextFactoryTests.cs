using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.Streaming;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Tests.Infrastructure;

public sealed class ScreenRuntimeContextFactoryTests
{
    [Fact]
    public void ScreenRuntimeContext_UsesInjectedServicesFactory()
    {
        var config = new VirtualScreenConfig
        {
            Port = 8000,
            ScreenSecurityEnabled = false,
            MaxViewers = 0,
            TouchInputEnabled = true,
            TouchHoldEnabled = true,
            TouchScrollEnabled = true,
            TouchPreserveCursor = false,
        };

        var factory = new FakeFactory();
        using var context = new ScreenRuntimeContext(
            "screen1",
            "Screen 1",
            config,
            "localhost",
            "127.0.0.1",
            new FakeDriverVerifier(),
            NullLoggerFactory.Instance,
            factory);

        Assert.True(factory.CaptureCreated);
        Assert.True(factory.EncoderCreated);
        Assert.True(factory.WebRtcCreated);
        Assert.Same(factory.CaptureService, context.FrameSource);
        Assert.Same(factory.WebRtcService, context.WebRtcStreamService);
        Assert.Equal(0, context.ViewerLimiter.ActiveCount);
    }

    private sealed class FakeFactory : IScreenRuntimeServicesFactory
    {
        internal FakeCaptureService CaptureService { get; } = new();
        internal FakeEncoderService EncoderService { get; } = new();
        internal FakeWebRtcService WebRtcService { get; } = new();

        internal bool CaptureCreated { get; private set; }
        internal bool EncoderCreated { get; private set; }
        internal bool WebRtcCreated { get; private set; }

        public IFrameCaptureService CreateCaptureService(VirtualScreenConfig config, ILoggerFactory loggerFactory, Func<string?> preferredDeviceNameProvider)
        {
            _ = config;
            _ = loggerFactory;
            _ = preferredDeviceNameProvider;
            CaptureCreated = true;
            return CaptureService;
        }

        public IH264EncoderService CreateH264EncoderService(IFrameCaptureService frameSource, VirtualScreenConfig config, ILoggerFactory loggerFactory)
        {
            _ = frameSource;
            _ = config;
            _ = loggerFactory;
            EncoderCreated = true;
            return EncoderService;
        }

        public IWebRtcStreamService CreateWebRtcStreamService(IH264EncoderService encoder, ILoggerFactory loggerFactory)
        {
            _ = encoder;
            _ = loggerFactory;
            WebRtcCreated = true;
            return WebRtcService;
        }

        public VirtualDisplayManager CreateDisplayManager(IDriverVerifier driverVerifier) =>
            new VirtualDisplayManager(driverVerifier);
    }

    private sealed class FakeCaptureService : IFrameCaptureService
    {
        public byte[] GetCurrentJpegFrame() => [];
        public void NotifyJpegDemand() { }
        public void EnterMjpegDemand() { }
        public void ExitMjpegDemand() { }
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

    private sealed class FakeDriverVerifier : IDriverVerifier
    {
        public (bool isAvailable, string statusMessage) Verify() => (true, "ok");
        public string InstallUrl => "https://example.test/driver";
        public string DriverName => "Fake Driver";
    }
}
