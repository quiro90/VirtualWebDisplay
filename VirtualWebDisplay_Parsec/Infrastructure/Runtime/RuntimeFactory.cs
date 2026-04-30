using Microsoft.Extensions.Logging;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Forms;

namespace VirtualWebDisplay.Infrastructure.Runtime;

/// <summary>
/// Construye los <see cref="ScreenRuntimeContext"/> para cada pantalla habilitada
/// y verifica que el driver de display virtual esté disponible si es necesario.
/// </summary>
internal static class RuntimeFactory
{
    /// <summary>
    /// Returns the enabled screen ports after verifying driver availability.
    /// Returns <c>null</c> if the driver is unavailable and the user cannot continue.
    /// Call this before building the DI container to configure Kestrel early.
    /// </summary>
    internal static IReadOnlyList<int>? GetEnabledPorts(VirtualWebDisplaySettings settings, IDriverVerifier driverVerifier)
    {
        var screens = new List<VirtualScreenConfig> { settings.Screen1 };
        if (settings.Screen2.Enabled)
            screens.Add(settings.Screen2);

        if (screens.Any(s => !VirtualDisplayPlacementOptions.IsDuplicate(s.VirtualDisplayPlacement)))
        {
            var (isAvailable, statusMessage) = driverVerifier.Verify();
            if (!isAvailable)
            {
                InstallDialog.Show(
                    StartupErrorMessages.TitleForDriverMissing(),
                    StartupErrorMessages.ForDriverUnavailable(statusMessage),
                    driverVerifier.InstallUrl);
                return null;
            }
        }

        return screens.Select(s => s.Port).ToList();
    }

    /// <summary>
    /// Crea la lista de runtimes a partir de la configuración actual.
    /// </summary>
    internal static List<ScreenRuntimeContext> TryCreate(
        VirtualWebDisplaySettings settings,
        string hostName,
        string localIp,
        IDriverVerifier driverVerifier,
        ILoggerFactory? loggerFactory = null)
    {
        var runtimes = new List<ScreenRuntimeContext>
        {
            new("screen1", AppText.Get("Runtime_Screen1"), settings.Screen1, hostName, localIp, driverVerifier, loggerFactory),
        };

        if (settings.Screen2.Enabled)
            runtimes.Add(new ScreenRuntimeContext("screen2", AppText.Get("Runtime_Screen2"), settings.Screen2, hostName, localIp, driverVerifier, loggerFactory));

        return runtimes;
    }
}
