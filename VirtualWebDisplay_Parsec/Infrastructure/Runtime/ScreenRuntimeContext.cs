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
        ILoggerFactory? loggerFactory = null)
    {
        Id = id;
        DisplayName = displayName;
        Config = config;
        DisplayManager = new VirtualDisplayManager(driverVerifier);
        _dxgiCaptureService = new DxgiCaptureService(
            config,
            (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DxgiCaptureService>(),
            () => DisplayManager.WindowsDeviceName);
        _h264Encoder = new H264EncoderService(_dxgiCaptureService, config, (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<H264EncoderService>());
        WebRtcStreamService = new WebRtcStreamService(_h264Encoder, (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<WebRtcStreamService>());
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
    private readonly DxgiCaptureService _dxgiCaptureService;
    private readonly H264EncoderService _h264Encoder;
    public WebRtcStreamService WebRtcStreamService { get; }
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
        catch
        {
        }

        try
        {
            await _h264Encoder.StopAsync(CancellationToken.None);
        }
        catch
        {
        }

        try
        {
            await _dxgiCaptureService.StopAsync(CancellationToken.None);
        }
        catch
        {
        }
    }
}

