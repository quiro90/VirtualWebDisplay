using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.Streaming;

namespace VirtualWebDisplay.Infrastructure;

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
        CaptureService = new CaptureService(config, (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<CaptureService>());
        WebRtcStreamService = new WebRtcStreamService(CaptureService, (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<WebRtcStreamService>());
        SecurityGate = new ScreenSecurityGate(config.ScreenSecurityEnabled);
        ViewerLimiter = new ViewerLimiter(config.MaxViewers);
        ViewerLimiter.GetWebRtcCount = () => WebRtcStreamService.ActivePeerCount;
        HostUrl = NetworkAddressHelper.BuildAccessUrl(hostName, config.Port);
        IpUrl = NetworkAddressHelper.BuildAccessUrl(localIp, config.Port);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public VirtualScreenConfig Config { get; }
    public VirtualDisplayManager DisplayManager { get; }
    public CaptureService CaptureService { get; }
    public WebRtcStreamService WebRtcStreamService { get; }
    public ScreenSecurityGate SecurityGate { get; }
    public ViewerLimiter ViewerLimiter { get; }
    public string HostUrl { get; }
    public string IpUrl { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await CaptureService.StartAsync(cancellationToken);
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
        CaptureService.Dispose();
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
            await CaptureService.StopAsync(CancellationToken.None);
        }
        catch
        {
        }
    }
}

