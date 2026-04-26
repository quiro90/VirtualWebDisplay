using Microsoft.Extensions.Logging.Abstractions;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.Streaming;

namespace VirtualWebDisplay.Infrastructure;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class ScreenRuntimeContext : IAsyncDisposable, IDisposable
{
    public ScreenRuntimeContext(string id, string displayName, VirtualScreenConfig config, string hostName, string localIp)
    {
        Id = id;
        DisplayName = displayName;
        Config = config;
        DisplayManager = new VirtualDisplayManager();
        CaptureService = new CaptureService(config);
        WebRtcStreamService = new WebRtcStreamService(CaptureService, config, NullLogger<WebRtcStreamService>.Instance);
        HostUrl = NetworkAddressHelper.BuildAccessUrl(hostName, config.Port);
        IpUrl = NetworkAddressHelper.BuildAccessUrl(localIp, config.Port);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public VirtualScreenConfig Config { get; }
    public VirtualDisplayManager DisplayManager { get; }
    public CaptureService CaptureService { get; }
    public WebRtcStreamService WebRtcStreamService { get; }
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

