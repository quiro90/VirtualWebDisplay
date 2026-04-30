using System.Security.Cryptography.X509Certificates;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
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
}
