using System.Windows.Forms;
using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Web.Handlers;

internal sealed class TouchInputCoordinateResolver
{
    internal const string MissingCoordinatesError = "Coordinates X and Y are required for this action.";

    internal bool TryResolveDesktopCoordinates(
        TouchInputRequest request,
        ScreenRuntimeContext runtime,
        out int desktopX,
        out int desktopY,
        out string errorMessage)
    {
        desktopX = 0;
        desktopY = 0;

        if (request.X is null || request.Y is null)
        {
            errorMessage = MissingCoordinatesError;
            return false;
        }

        var targetBounds = ResolveTargetMonitorBounds(runtime);
        var (screenX, screenY) = MapCoordinates(
            request.X.Value,
            request.Y.Value,
            request.ViewportWidth ?? 1.0,
            request.ViewportHeight ?? 1.0,
            targetBounds.Width,
            targetBounds.Height);

        desktopX = targetBounds.Left + screenX;
        desktopY = targetBounds.Top + screenY;

        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] Bounds({targetBounds.Left},{targetBounds.Top},{targetBounds.Width}x{targetBounds.Height}) " +
            $"Config({runtime.Config.Width}x{runtime.Config.Height}) -> desktop({desktopX},{desktopY})");

        errorMessage = string.Empty;
        return true;
    }

    private static (int screenX, int screenY) MapCoordinates(
        double viewportX,
        double viewportY,
        double viewportWidth,
        double viewportHeight,
        int screenWidth,
        int screenHeight)
    {
        var result = InputCoordinateMapper.Map(viewportX, viewportY, viewportWidth, viewportHeight, screenWidth, screenHeight);
        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] MapCoordinates: viewport({viewportX:F1},{viewportY:F1}) -> localScreen({result.screenX},{result.screenY})");
        return result;
    }

    private static System.Drawing.Rectangle ResolveTargetMonitorBounds(ScreenRuntimeContext runtime)
    {
        var screens = Screen.AllScreens;

        if (!string.IsNullOrWhiteSpace(runtime.DisplayManager.WindowsDeviceName))
        {
            var matchByName = screens.FirstOrDefault(s =>
                string.Equals(s.DeviceName, runtime.DisplayManager.WindowsDeviceName, StringComparison.OrdinalIgnoreCase));
            if (matchByName is not null)
                return matchByName.Bounds;
        }

        if (runtime.Config.MonitorIndex >= 0 && runtime.Config.MonitorIndex < screens.Length)
            return screens[runtime.Config.MonitorIndex].Bounds;

        return Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, runtime.Config.Width, runtime.Config.Height);
    }
}
