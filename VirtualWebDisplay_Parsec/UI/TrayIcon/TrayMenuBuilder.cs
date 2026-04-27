using System.Diagnostics;
using System.Windows.Forms;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.TrayIcon;

/// <summary>
/// Builds the tray icon's ContextMenuStrip.
/// Pure static class: receives all state as parameters and has no side effects.
/// </summary>
internal static class TrayMenuBuilder
{
    internal static ContextMenuStrip Build(
        IReadOnlyList<ScreenRuntimeContext> screenRuntimes,
        bool serviceActionPending,
        TaskCompletionSource<bool>? serviceStartSignal,
        Action onShowConfiguration,
        Action onStopService,
        Action onStartService,
        Action onExit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(AppText.Get("Tray_Menu_Configuration"), null, (_, _) => onShowConfiguration());

        if (screenRuntimes.Count > 0)
        {
            menu.Items.Add(new ToolStripSeparator());
            foreach (var runtime in screenRuntimes)
                menu.Items.Add(AppText.Format("Tray_Menu_OpenDisplay", runtime.DisplayName), null, (_, _) => OpenStreamUrl(runtime.HostUrl));
            menu.Items.Add(new ToolStripSeparator());
        }

        if (screenRuntimes.Count > 0 && !serviceActionPending)
            menu.Items.Add(AppText.Get("Tray_Menu_Stop"), null, (_, _) => onStopService());
        else if (screenRuntimes.Count == 0 && serviceStartSignal is not null && !serviceActionPending)
            menu.Items.Add(AppText.Get("Tray_Menu_Start"), null, (_, _) => onStartService());

        menu.Items.Add(AppText.Get("Tray_Menu_Exit"), null, (_, _) => onExit());
        return menu;
    }

    private static void OpenStreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
