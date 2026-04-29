using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Infrastructure.Messaging;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Forms;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Inicializa los runtimes de pantalla: crea los displays virtuales necesarios, asigna los
/// índices de monitor y arranca los servicios de captura. Devuelve <c>false</c> si algún
/// runtime no pudo iniciarse (ya muestra el diálogo de error correspondiente).
/// </summary>
internal static class RuntimeStartupHelper
{
    public static async Task<bool> StartRuntimesAsync(IReadOnlyList<ScreenRuntimeContext> runtimes, IDriverVerifier driverVerifier)
    {
        foreach (var runtime in runtimes)
        {
            if (VirtualDisplayPlacementOptions.IsDuplicate(runtime.Config.VirtualDisplayPlacement))
            {
                var primaryIndex = Array.FindIndex(Screen.AllScreens, s => s.Primary);
                // MonitorIndex is resolved at startup time; config.MonitorIndex=-1 means "auto" until this point.
                runtime.Config.MonitorIndex = primaryIndex >= 0 ? primaryIndex : 0;
                await runtime.StartAsync(CancellationToken.None);
                continue;
            }

            var (ok, vddStatus) = runtime.DisplayManager.TryCreate(runtime.Config);
            if (!ok)
            {
                await RuntimeCleanupHelper.DisposeRuntimesAsync(runtimes);
                InstallDialog.Show(
                    StartupErrorMessages.TitleForDisplayError(runtime.DisplayName),
                    StartupErrorMessages.ForDisplayCreationFailure(vddStatus),
                    driverVerifier.InstallUrl);
                return false;
            }

            if (runtime.DisplayManager.WindowsMonitorIndex is int virtualMonitorIndex)
            {
                // MonitorIndex is resolved here once the virtual display is registered by Windows.
                runtime.Config.MonitorIndex = virtualMonitorIndex;
            }
            else if (runtime.Config.MonitorIndex < 0)
            {
                await RuntimeCleanupHelper.DisposeRuntimesAsync(runtimes);
                MessageBox.Show(
                    StartupErrorMessages.ForMonitorNotDetected(vddStatus, runtime.DisplayName),
                    AppText.Get("Program_MonitorNotDetected_Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            await runtime.StartAsync(CancellationToken.None);
        }

        return true;
    }
}
