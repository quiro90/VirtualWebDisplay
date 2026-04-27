using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Controllers;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.TrayIcon;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Gestiona el ciclo de vida de la aplicación: bucle principal de inicio/parada,
/// coordinación con el tray icon y limpieza de recursos al salir.
/// </summary>
internal static class ApplicationLifecycleManager
{
    internal static async Task RunAsync(
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
        var keepRunning = true;
        while (keepRunning)
        {
            // Resolve ports from settings before building the app, so Kestrel can be
            // configured without creating the full runtimes twice.
            var enabledPorts = RuntimeFactory.GetEnabledPorts(settings);
            if (enabledPorts is null)
                return;

            var builder = WebApplication.CreateBuilder(args);
            KestrelConfigurator.Configure(builder, enabledPorts, tlsCert);

            var app = builder.Build();
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

            var runtimes = RuntimeFactory.TryCreate(settings, hostName, localIp, loggerFactory);
            if (runtimes is null)
                return;

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
                tray.NotifyServiceStopped();
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

            keepRunning = false;
        }
    }
}
