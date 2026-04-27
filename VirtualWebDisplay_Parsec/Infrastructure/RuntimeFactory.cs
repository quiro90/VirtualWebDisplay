using Microsoft.Extensions.Logging;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.UI.Forms;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Construye los <see cref="ScreenRuntimeContext"/> para cada pantalla habilitada
/// y verifica que el driver de Parsec VDD esté disponible si es necesario.
/// </summary>
internal static class RuntimeFactory
{
    /// <summary>
    /// Returns the enabled screen ports after verifying driver availability.
    /// Returns <c>null</c> if the driver is unavailable and the user cannot continue.
    /// Call this before building the DI container to configure Kestrel early.
    /// </summary>
    internal static IReadOnlyList<int>? GetEnabledPorts(VirtualWebDisplaySettings settings)
    {
        var screens = new List<VirtualScreenConfig> { settings.Screen1 };
        if (settings.Screen2.Enabled)
            screens.Add(settings.Screen2);

        if (screens.Any(s => !VirtualDisplayPlacementOptions.IsDuplicate(s.VirtualDisplayPlacement)))
        {
            var (driverReady, driverStatus) = VirtualDisplayManager.VerifyDriverAvailability();
            if (!driverReady)
            {
                InstallDialog.Show(
                    AppText.Get("Program_DriverMissing_Title"),
                    driverStatus + "\n\n" + AppText.Get("Program_DriverMissing_MessageSuffix"),
                    VirtualDisplayManager.InstallUrl);
                return null;
            }
        }

        return screens.Select(s => s.Port).ToList();
    }

    /// <summary>
    /// Crea la lista de runtimes a partir de la configuración actual.
    /// Devuelve <c>null</c> si el driver no está disponible y el usuario no puede continuar.
    /// </summary>
    internal static List<ScreenRuntimeContext>? TryCreate(
        VirtualWebDisplaySettings settings,
        string hostName,
        string localIp,
        ILoggerFactory? loggerFactory = null)
    {
        var runtimes = new List<ScreenRuntimeContext>
        {
            new("screen1", AppText.Get("Runtime_Screen1"), settings.Screen1, hostName, localIp, loggerFactory),
        };

        if (settings.Screen2.Enabled)
            runtimes.Add(new ScreenRuntimeContext("screen2", AppText.Get("Runtime_Screen2"), settings.Screen2, hostName, localIp, loggerFactory));

        return runtimes;
    }
}
