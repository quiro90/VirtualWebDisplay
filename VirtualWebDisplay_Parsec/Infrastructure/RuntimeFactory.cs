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

        if (runtimes.Any(r => !VirtualDisplayPlacementOptions.IsDuplicate(r.Config.VirtualDisplayPlacement)))
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

        return runtimes;
    }
}
