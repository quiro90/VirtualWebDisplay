using System.Security.Cryptography.X509Certificates;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Infrastructure.Updates;
using VirtualWebDisplay.UI.Forms;
using VirtualWebDisplay.UI.Theme;
using VirtualWebDisplay.UI.TrayIcon;

namespace VirtualWebDisplay.Infrastructure.Hosting;

/// <summary>
/// Orquesta el inicio completo de la aplicación: verificación de driver,
/// configuración de pantallas, y preparación del servidor web.
/// Delega el ciclo de vida del servicio a <see cref="ApplicationLifecycleManager"/>.
/// </summary>
internal static class ApplicationBootstrapper
{
    /// <summary>
    /// Inicia la aplicación: verifica driver, crea runtimes y arranca el ciclo de vida.
    /// </summary>
    public static async Task RunAsync(
        VirtualDisplayTrayController tray,
        VirtualWebDisplaySettings settings,
        AppearanceSettingsStore appearanceStore,
        VirtualDisplayResolutionStore resolutionStore,
        SingleInstanceManager singleInstance,
        string[] args,
        X509Certificate2 tlsCert,
        byte[] tlsCertDerBytes,
        string hostName,
        string localIp)
    {
        var driverVerifier = new ParsecVddDriverVerifier();

        // Verificar disponibilidad del driver antes de crear el servidor
        var enabledPorts = RuntimeFactory.GetEnabledPorts(settings, driverVerifier);
        if (enabledPorts is null)
            return; // Usuario canceló o driver no disponible

        // Delegar ciclo de vida al manager existente (mantiene loop de reinicio)
        await ApplicationLifecycleManager.RunServiceLoopAsync(
            tray, settings, appearanceStore, resolutionStore, singleInstance,
            args, tlsCert, tlsCertDerBytes, hostName, localIp,
            enabledPorts, driverVerifier);
    }

    /// <summary>
    /// Checks GitHub for a newer release and shows the update dialog if one is found.
    /// Runs in the background — never blocks startup.
    /// </summary>
    internal static async Task CheckForUpdateInBackgroundAsync(
        VirtualDisplayTrayController tray,
        AppearanceSettingsStore appearanceStore)
    {
        try
        {
            // Pequeño delay para no bloquear el arranque visual.
            await Task.Delay(TimeSpan.FromSeconds(5));

            var release = await UpdateCheckService.CheckForUpdateAsync();
            if (release is null)
                return;

            tray.InvokeOnUiThread(() =>
            {
                var appearance = appearanceStore.Load();
                var isDark     = FormThemeApplicator.ResolveDarkMode(appearance.WindowTheme);
                var palette    = isDark ? ThemePalette.Dark() : ThemePalette.Light();

                UpdateAvailableDialog.Show(
                    owner:           null,
                    release:         release,
                    backgroundColor: palette.Background,
                    foregroundColor: palette.Foreground,
                    panelColor:      palette.Panel,
                    borderColor:     palette.Border,
                    linkColor:       palette.Link,
                    linkActiveColor: palette.LinkActive);
            });
        }
        catch (Exception ex)
        {
            // Fail silently — update check must never crash the app.
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ApplicationBootstrapper] Background update check failed: {ex.Message}");
#endif
        }
    }
}
