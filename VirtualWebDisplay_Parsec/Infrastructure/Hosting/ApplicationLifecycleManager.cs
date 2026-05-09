using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.TrayIcon;

namespace VirtualWebDisplay.Infrastructure.Hosting;

/// <summary>
/// Gestiona el ciclo de vida de la aplicación: bucle principal de inicio/parada,
/// coordinación con el tray icon y limpieza de recursos al salir.
/// </summary>
internal static class ApplicationLifecycleManager
{
    internal static async Task RunServiceLoopAsync(
        VirtualDisplayTrayController tray,
        VirtualWebDisplaySettings settings,
        AppearanceSettingsStore appearanceStore,
        VirtualDisplayResolutionStore resolutionStore,
        SingleInstanceManager singleInstance,
        string[] args,
        X509Certificate2 tlsCert,
        byte[] tlsCertDerBytes,
        string hostName,
        string localIp,
        IReadOnlyList<int> enabledPorts,
        IDriverVerifier driverVerifier)
    {
        var keepRunning = true;
        while (keepRunning)
        {
            var builder = WebApplication.CreateBuilder(args);
            KestrelConfigurator.Configure(builder, enabledPorts, tlsCert);

            var app = builder.Build();
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

            var runtimes = RuntimeFactory.TryCreate(settings, hostName, localIp, driverVerifier, loggerFactory);

            singleInstance.StartShutdownListener(() => app.Lifetime.StopApplication());
            var stopRequested = false;
            var exitRequested = false;

            try
            {
                if (!await RuntimeStartupHelper.StartRuntimesAsync(runtimes, driverVerifier))
                    return;

                tray.ConfigureRuntimeActions(
                    exitRequested: () => { exitRequested = true; app.Lifetime.StopApplication(); },
                    stopRequested: () => { stopRequested = true; app.Lifetime.StopApplication(); },
                    screenRuntimes: runtimes);

                // Habilitar archivos estáticos (JavaScript, CSS, etc.)
                app.UseStaticFiles();

                WebApiEndpoints.Map(app, runtimes, tlsCertDerBytes);

                using var resolutionWatcher = new VirtualResolutionWatcher(runtimes, resolutionStore);
                resolutionWatcher.RestoreOrSeedResolutions();
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
                // Notificar que el servicio se detuvo (actualiza estado y notifica formularios).
                tray.NotifyServiceStopped();

                // Solo esperar señal de reinicio si el usuario no pidió salir.
                // Si exitRequested = true el tray ya está cerrado y nadie llamará a
                // SignalNoRestart(); esperar aquí colgaría el proceso indefinidamente.
                if (!exitRequested)
                {
                    var startAgain = await tray.WaitForServiceStartAsync();
                    if (startAgain)
                    {
                        var appearance = appearanceStore.Load();
                        AppText.ApplyCulture(appearance.UiLanguage);
                        settings.UiLanguage = appearance.UiLanguage;
                        settings.WindowTheme = appearance.WindowTheme;
                        await Task.Delay(500);
                        continue;
                    }
                }
            }

            keepRunning = false;
        }
    }

}
