using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// Installs the bundled VDD driver using pnputil with UAC elevation.
/// </summary>
public static class VddDriverInstaller
{
    private const string DriverSubdir = "vdd";
    private const string InfFileName  = "MttVDD.inf";

    public enum InstallResult
    {
        Success,
        RebootRequired,
        Cancelled,
        FilesNotFound,
        Failed,
    }

    /// <summary>Returns the path to the bundled .inf, or null if not present.</summary>
    public static string? GetBundledInfPath()
    {
        var inf = Path.Combine(AppContext.BaseDirectory, DriverSubdir, InfFileName);
        return File.Exists(inf) ? inf : null;
    }

    /// <summary>
    /// Installs the driver via pnputil /add-driver MttVDD.inf /install.
    /// Prompts UAC elevation.
    /// </summary>
    public static InstallResult Install()
    {
        var infPath = GetBundledInfPath();
        if (infPath is null)
            return InstallResult.FilesNotFound;

        var psi = new ProcessStartInfo
        {
            FileName        = "pnputil.exe",
            Arguments       = $"/add-driver \"{infPath}\" /install",
            Verb            = "runas",
            UseShellExecute = true,
            CreateNoWindow  = false,
        };

        Process process;
        try
        {
            process = Process.Start(psi)!;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return InstallResult.Cancelled;
        }
        catch
        {
            return InstallResult.Failed;
        }

        process.WaitForExit();
        return process.ExitCode switch
        {
            0    => InstallResult.Success,
            3010 => InstallResult.RebootRequired,
            _    => InstallResult.Failed,
        };
    }

    /// <summary>
    /// Polls VirtualDisplayManager until a VDD adapter appears or the timeout elapses.
    /// </summary>
    public static bool WaitForDriver(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var (ready, _) = VirtualDisplayManager.VerifyDriverAvailability();
            if (ready)
                return true;

            Thread.Sleep(500);
        }

        return false;
    }
}
