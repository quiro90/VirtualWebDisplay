using System.Net;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
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

await ApplicationLifecycleManager.RunAsync(
    tray, settings, appearanceStore, singleInstance,
    args, tlsCert, tlsCertDerBytes, hostName, localIp);

singleInstance.Dispose();
Environment.Exit(0);
