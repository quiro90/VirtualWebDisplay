using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.Streaming;

namespace VirtualWebDisplay.Infrastructure.Runtime;

public sealed class ScreenRuntimeContext : IAsyncDisposable, IDisposable
{
    public ScreenRuntimeContext(
        string id, 
        string displayName, 
        VirtualScreenConfig config, 
        string hostName, 
        string localIp, 
        IDriverVerifier driverVerifier,
        ILoggerFactory? loggerFactory = null,
        IScreenRuntimeServicesFactory? servicesFactory = null)
    {
        servicesFactory ??= DefaultScreenRuntimeServicesFactory.Instance;
        var effectiveLoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        Id = id;
        DisplayName = displayName;
        Config = config;
        DisplayManager = servicesFactory.CreateDisplayManager(driverVerifier);
        _dxgiCaptureService = servicesFactory.CreateCaptureService(
            config,
            effectiveLoggerFactory,
            () => DisplayManager.WindowsDeviceName);
        _h264Encoder = servicesFactory.CreateH264EncoderService(_dxgiCaptureService, config, effectiveLoggerFactory);
        WebRtcStreamService = servicesFactory.CreateWebRtcStreamService(_h264Encoder, effectiveLoggerFactory);
        SecurityGate = new ScreenSecurityGate(config.ScreenSecurityEnabled);
        ViewerLimiter = new ViewerLimiter(config.MaxViewers);
        ViewerLimiter.GetWebRtcCount = () => WebRtcStreamService.ActivePeerCount;
        HostUrl = NetworkAddressHelper.BuildAccessUrl(hostName, config.Port);
        IpUrl = NetworkAddressHelper.BuildAccessUrl(localIp, config.Port);
        CapToken = Guid.NewGuid().ToString("N")[..16];
    }

    public string Id { get; }
    public string DisplayName { get; }
    public VirtualScreenConfig Config { get; }
    public VirtualDisplayManager DisplayManager { get; }
    internal IFrameSource FrameSource => _dxgiCaptureService;
    private readonly IFrameCaptureService _dxgiCaptureService;
    private readonly IH264EncoderService _h264Encoder;
    public IWebRtcStreamService WebRtcStreamService { get; }
    public ScreenSecurityGate SecurityGate { get; }
    public ViewerLimiter ViewerLimiter { get; }
    public string HostUrl { get; }
    public string IpUrl { get; }
    public string CapToken { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _dxgiCaptureService.StartAsync(cancellationToken);
        await _h264Encoder.StartAsync(cancellationToken);
        await WebRtcStreamService.StartAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        Dispose();
    }

    public void Dispose()
    {
        WebRtcStreamService.Dispose();
        _h264Encoder.Dispose();
        _dxgiCaptureService.Dispose();
        DisplayManager.Dispose();
    }

    public async Task StopAsync()
    {
        try
        {
            await WebRtcStreamService.StopAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
    #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ScreenRuntimeContext:{Id}] WebRtcStreamService stop failed: {ex.Message}");
    #endif
        }

        try
        {
            await _h264Encoder.StopAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
    #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ScreenRuntimeContext:{Id}] H264EncoderService stop failed: {ex.Message}");
    #endif
        }

        try
        {
            await _dxgiCaptureService.StopAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
    #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ScreenRuntimeContext:{Id}] DxgiCaptureService stop failed: {ex.Message}");
    #endif
        }
    }
}

