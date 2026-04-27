using System.Windows.Forms;

namespace VirtualWebDisplay.Infrastructure;

public static class RuntimeCleanupHelper
{
    public static async Task DisposeRuntimesAsync(IEnumerable<ScreenRuntimeContext> runtimes)
    {
        foreach (var runtime in runtimes.Reverse())
            await runtime.DisposeAsync();
    }

    public static async Task WaitForVirtualDisplaysRemovalAsync(IReadOnlyCollection<string> deviceNames, TimeSpan timeout)
    {
        if (deviceNames.Count == 0)
            return;

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var remaining = Screen.AllScreens
                .Select(screen => screen.DeviceName)
                .Where(name => deviceNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (remaining.Length == 0)
                return;

            await Task.Delay(120);
        }
    }
}
