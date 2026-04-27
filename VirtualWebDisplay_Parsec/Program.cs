using System.Net;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Controllers;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.UI.Forms;
using VirtualWebDisplay.UI.TrayIcon;

var appearanceStore = new AppearanceSettingsStore();
var appearance = appearanceStore.Load();
AppText.ApplyCulture(appearance.UiLanguage);

var settingsStore = new VirtualScreenSettingsStore();
var settings = settingsStore.Load();
settings.EnsureValid();
settings.UiLanguage = appearance.UiLanguage;
settings.WindowTheme = appearance.WindowTheme;

using var singleInstance = SingleInstanceManager.CreateForCurrentExecutable();
if (!singleInstance.EnsureSingleInstance(TimeSpan.FromSeconds(10)))
{
    MessageBox.Show(
        AppText.Get("Program_SingleInstanceCloseFailed_Message"),
        AppText.Get("Program_SingleInstanceCloseFailed_Title"),
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
    return;
}

var localIp = NetworkAddressHelper.DetectLocalIp();
var hostName = Dns.GetHostName();

using var tray = new VirtualDisplayTrayController(settings, settingsStore, appearanceStore, localIp);

var autoStart = args.Contains("--autostart", StringComparer.OrdinalIgnoreCase);
if (!autoStart && !tray.ShowStartupConfiguration())
    return;

settings.EnsureValid();
appearance = appearanceStore.Load();
AppText.ApplyCulture(appearance.UiLanguage);
settings.UiLanguage = appearance.UiLanguage;
settings.WindowTheme = appearance.WindowTheme;

var certStoreDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    VirtualScreenSettingsStore.DirectoryName);

var (tlsCert, tlsCertDerBytes) = LocalCertificateProvider.GetOrCreate(certStoreDir, localIp, hostName);

var keepRunning = true;
while (keepRunning)
{
    var runtimes = new List<ScreenRuntimeContext>
    {
        new("screen1", AppText.Get("Runtime_Screen1"), settings.Screen1, hostName, localIp),
    };
    if (settings.Screen2.Enabled)
        runtimes.Add(new ScreenRuntimeContext("screen2", AppText.Get("Runtime_Screen2"), settings.Screen2, hostName, localIp));

    if (runtimes.Any(r => !VirtualDisplayPlacementOptions.IsDuplicate(r.Config.VirtualDisplayPlacement)))
    {
        var (driverReady, driverStatus) = VirtualDisplayManager.VerifyDriverAvailability();
        if (!driverReady)
        {
            InstallDialog.Show(
                AppText.Get("Program_DriverMissing_Title"),
                driverStatus + "\n\n" + AppText.Get("Program_DriverMissing_MessageSuffix"),
                VirtualDisplayManager.InstallUrl);
            return;
        }
    }

    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        foreach (var runtime in runtimes)
        {
            kestrel.ListenAnyIP(runtime.Config.Port);
            kestrel.ListenAnyIP(runtime.Config.Port + 1, listenOptions =>
                listenOptions.UseHttps(tlsCert));
        }
    });

    var app = builder.Build();
    singleInstance.StartShutdownListener(() => app.Lifetime.StopApplication());
    var stopRequested = false;
    var exitRequested = false;

    try
    {
        if (!await RuntimeStartupHelper.StartRuntimesAsync(runtimes))
            return;

        tray.ConfigureRuntimeActions(
            () => { exitRequested = true; app.Lifetime.StopApplication(); },
            () => { stopRequested = true; app.Lifetime.StopApplication(); },
            runtimes);

        WebApiEndpoints.Map(app, runtimes, tlsCertDerBytes);

        await app.RunAsync();
    }
    finally
    {
        var createdVirtualDeviceNames = runtimes
            .Select(r => r.DisplayManager.WindowsDeviceName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await RuntimeCleanupHelper.DisposeRuntimesAsync(runtimes);

        if (stopRequested || exitRequested)
            await RuntimeCleanupHelper.WaitForVirtualDisplaysRemovalAsync(createdVirtualDeviceNames, TimeSpan.FromSeconds(6));
    }

    if (stopRequested)
    {
        tray.NotifyServiceStopped();
        var startAgain = await tray.WaitForServiceStartAsync();
        if (startAgain)
        {
            appearance = appearanceStore.Load();
            AppText.ApplyCulture(appearance.UiLanguage);
            settings.UiLanguage = appearance.UiLanguage;
            settings.WindowTheme = appearance.WindowTheme;
            await Task.Delay(500); // allow OS to release port bindings
            continue;
        }
    }

    keepRunning = false;
}

singleInstance.Dispose();
Environment.Exit(0);
