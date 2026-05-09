using Microsoft.Extensions.Logging;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.Streaming;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Infrastructure.Runtime;

public interface IFrameCaptureService : IFrameSource, IDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public interface IH264EncoderService : IDisposable
{
    event Action<byte[], long>? NalUnitReady;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public interface IWebRtcStreamService : IDisposable
{
    int ActivePeerCount { get; }
    Task<WebRtcSessionAnswer> CreateAnswerAsync(WebRtcSessionOffer offer, CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public interface IScreenRuntimeServicesFactory
{
    IFrameCaptureService CreateCaptureService(
        VirtualScreenConfig config,
        ILoggerFactory loggerFactory,
        Func<string?> preferredDeviceNameProvider);

    IH264EncoderService CreateH264EncoderService(
        IFrameCaptureService frameSource,
        VirtualScreenConfig config,
        ILoggerFactory loggerFactory);

    IWebRtcStreamService CreateWebRtcStreamService(
        IH264EncoderService encoder,
        ILoggerFactory loggerFactory);

    VirtualDisplayManager CreateDisplayManager(IDriverVerifier driverVerifier);
}

internal sealed class DefaultScreenRuntimeServicesFactory : IScreenRuntimeServicesFactory
{
    internal static readonly DefaultScreenRuntimeServicesFactory Instance = new();

    public IFrameCaptureService CreateCaptureService(
        VirtualScreenConfig config,
        ILoggerFactory loggerFactory,
        Func<string?> preferredDeviceNameProvider) =>
        new DxgiCaptureService(
            config,
            loggerFactory.CreateLogger<DxgiCaptureService>(),
            preferredDeviceNameProvider);

    public IH264EncoderService CreateH264EncoderService(
        IFrameCaptureService frameSource,
        VirtualScreenConfig config,
        ILoggerFactory loggerFactory) =>
        new H264EncoderService(
            frameSource,
            config,
            loggerFactory.CreateLogger<H264EncoderService>());

    public IWebRtcStreamService CreateWebRtcStreamService(
        IH264EncoderService encoder,
        ILoggerFactory loggerFactory) =>
        new WebRtcStreamService(
            encoder,
            loggerFactory.CreateLogger<WebRtcStreamService>());

    public VirtualDisplayManager CreateDisplayManager(IDriverVerifier driverVerifier) =>
        new(driverVerifier);
}
