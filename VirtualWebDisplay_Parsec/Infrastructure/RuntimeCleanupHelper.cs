using System.Windows.Forms;
using VirtualWebDisplay.Infrastructure.Polling;

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

        await PollingHelper.WaitUntilAsync(
            condition: () =>
            {
                var remaining = Screen.AllScreens
                    .Select(screen => screen.DeviceName)
                    .Where(name => deviceNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
                return remaining.Length == 0;
            },
            timeout: timeout,
            pollInterval: TimeSpan.FromMilliseconds(120));
    }
}
