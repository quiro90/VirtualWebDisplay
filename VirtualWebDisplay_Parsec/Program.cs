using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Infrastructure.Hosting;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.UI.TrayIcon;

// ── Modo UAC: solo escribe modos custom al registro y sale ───────────────────
if (TryGetCustomModesArgument(args, out var customModesArg))
{
    try
    {
        var modes = ParseCustomModesArgument(customModesArg);
        VddCustomModesStore.Write(modes);
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "VirtualWebDisplay", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    return;
}
// ─────────────────────────────────────────────────────────────────────────────

// ── Instancia Única de UI ────────────────────────────────────────────────────
var processPath = Path.GetFullPath(Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory);
var pathHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(processPath)))[..24];

using var uiActivator = new SingleInstanceActivator($"VirtualWebDisplay_UI_{pathHash}");

if (!uiActivator.IsFirstInstance)
{
    uiActivator.SignalFirstInstanceAndExit();
    return;
}
// ─────────────────────────────────────────────────────────────────────────────

var appearanceStore = new AppearanceSettingsStore();
var appearance = appearanceStore.Load();
AppText.ApplyCulture(appearance.UiLanguage);

var settingsStore = new VirtualScreenSettingsStore();
var resolutionStore = new VirtualDisplayResolutionStore();
var settings = settingsStore.Load();
settings.EnsureValid();
settings.UiLanguage = appearance.UiLanguage;
settings.WindowTheme = appearance.WindowTheme;

// Usamos el namespace completo para resolver la ambigüedad con el nuevo SingleInstanceActivator
using var serviceLifecycleManager = VirtualWebDisplay.Infrastructure.Hosting.SingleInstanceManager.CreateForCurrentExecutable();
if (!serviceLifecycleManager.EnsureSingleInstance(TimeSpan.FromSeconds(10)))
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

using var tray = new VirtualDisplayTrayController(
    uiActivator,
    settings,
    settingsStore,
    appearanceStore,
    localIp);

// Chequear actualizaciones en background apenas inicia la app visual.
_ = ApplicationBootstrapper.CheckForUpdateInBackgroundAsync(tray, appearanceStore);

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

await ApplicationBootstrapper.RunAsync(
    tray, settings, appearanceStore, resolutionStore, serviceLifecycleManager,
    args, tlsCert, tlsCertDerBytes, hostName, localIp);

serviceLifecycleManager.Dispose();
Environment.Exit(0);

static bool TryGetCustomModesArgument(string[] args, out string modesArg)
{
    if (args.Length >= 2 && args[0] == "--set-custom-modes")
    {
        modesArg = args[1];
        return true;
    }

    modesArg = string.Empty;
    return false;
}

static List<VddCustomModesStore.CustomMode> ParseCustomModesArgument(string modesArg) =>
    modesArg
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Select(entry =>
        {
            var atIdx = entry.IndexOf('@');
            var xIdx  = entry.IndexOf('x');
            var w  = int.Parse(entry[..xIdx]);
            var h  = int.Parse(entry[(xIdx + 1)..atIdx]);
            var hz = int.Parse(entry[(atIdx + 1)..]);
            return new VddCustomModesStore.CustomMode(w, h, hz);
        })
        .ToList();
