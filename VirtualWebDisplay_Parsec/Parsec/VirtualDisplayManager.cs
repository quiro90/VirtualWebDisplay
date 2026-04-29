using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Infrastructure.Polling;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Parsec;

/// <summary>
/// Manages a Parsec Virtual Display by talking directly to the installed driver.
/// No external DLL is required next to this executable.
/// </summary>
public sealed class VirtualDisplayManager : IDisposable
{
    private const string AdapterGuid = "{00b41627-04c4-429e-a26e-0265cf50c8fa}";
    private readonly IDriverVerifier _driverVerifier;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public POINTL dmPosition;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int DM_POSITION = 0x00000020;
    private const int DM_PELSWIDTH = 0x00080000;
    private const int DM_PELSHEIGHT = 0x00100000;
    private const uint CDS_UPDATEREGISTRY = 0x00000001;
    private const uint CDS_NORESET = 0x10000000;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DISP_CHANGE_BADMODE = -2;
    private IntPtr _handle = IntPtr.Zero;
    private int _displayIdx = -1;
    private CancellationTokenSource? _keepAliveCancellation;
    private Task? _keepAliveTask;

    public VirtualDisplayManager(IDriverVerifier driverVerifier)
    {
        _driverVerifier = driverVerifier;
    }

    public bool IsActive { get; private set; }
    public int? WindowsMonitorIndex { get; private set; }
    public string? WindowsDeviceName { get; private set; }

    public (bool ok, string message) TryCreate(VirtualScreenConfig config)
    {
        try
        {
            var screensBefore = Screen.AllScreens;
            var screenNamesBefore = screensBefore
                .Select(s => s.DeviceName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!ParsecVddDriverApi.OpenHandle(AdapterGuid, out _handle))
            {
                var (isAvailable, statusMessage) = _driverVerifier.Verify();
                return (isAvailable, statusMessage);
            }

            if (!ParsecVddDriverApi.AddDisplay(_handle, out _displayIdx))
            {
                Reset();
                return (false, AppText.Get("Parsec_Driver_CreateDisplayFailed"));
            }

            StartKeepAlive();

            Screen? virtualScreen = null;

            var found = PollingHelper.WaitUntil(
                condition: () =>
                {
                    var screensNow = Screen.AllScreens;
                    virtualScreen = screensNow.FirstOrDefault(s => !screenNamesBefore.Contains(s.DeviceName));
                    return virtualScreen is not null;
                },
                timeout: TimeSpan.FromSeconds(5),
                pollInterval: TimeSpan.FromMilliseconds(100));

            if (!found && Screen.AllScreens.Length > screensBefore.Length)
                virtualScreen = Screen.AllScreens[^1];

            string arrangeStatus;
            if (virtualScreen is not null)
            {
                WindowsDeviceName = virtualScreen.DeviceName;
                var arrangeResult = ArrangeVirtualDisplay(virtualScreen.DeviceName, config);
                if (!arrangeResult.ok)
                    return (false, arrangeResult.message);

                arrangeStatus = arrangeResult.message;

                Thread.Sleep(250);
                var screensNow = Screen.AllScreens;
                UpdateAppliedDisplayMetrics(config, screensNow);
            }
            else
            {
                arrangeStatus = AppText.Get("VDD_Status_MonitorNotIdentified");
            }

            IsActive = true;
            return (true, AppText.Format("VDD_Status_VirtualMonitorCreated",
                _displayIdx,
                Screen.AllScreens.Length,
                WindowsMonitorIndex is int idx ? AppText.Format("VDD_Status_VirtualMonitorMonitorIndexSuffix", idx) : string.Empty,
                arrangeStatus));
        }
        catch (Exception ex)
        {
            Reset();
            return (false, AppText.Format("VDD_Status_UnexpectedError", ex.Message));
        }
    }

    private void StartKeepAlive()
    {
        _keepAliveCancellation?.Dispose();
        _keepAliveCancellation = new CancellationTokenSource();
        var token = _keepAliveCancellation.Token;

        _keepAliveTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (ParsecVddDriverApi.IsValidHandle(_handle))
                        ParsecVddDriverApi.Update(_handle);

                    await Task.Delay(100, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(250, token);
                }
            }
        }, token);
    }

    private void StopKeepAlive()
    {
        if (_keepAliveCancellation is null)
            return;

        _keepAliveCancellation.Cancel();

        try
        {
            _keepAliveTask?.Wait(500);
        }
        catch
        {
        }

        _keepAliveTask = null;
        _keepAliveCancellation.Dispose();
        _keepAliveCancellation = null;
    }

    private void Reset()
    {
        StopKeepAlive();

        if (ParsecVddDriverApi.IsValidHandle(_handle))
            ParsecVddDriverApi.CloseHandle(_handle);

        _handle = IntPtr.Zero;
        _displayIdx = -1;
        WindowsMonitorIndex = null;
        WindowsDeviceName = null;
        IsActive = false;
    }

    public (bool ok, string message) TryReconfigure(VirtualScreenConfig config)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(WindowsDeviceName))
            return (false, AppText.Get("VDD_Status_NotReadyToReconfigure"));

        try
        {
            var result = ArrangeVirtualDisplay(WindowsDeviceName, config);
            Thread.Sleep(250);

            var screensNow = Screen.AllScreens;
            UpdateAppliedDisplayMetrics(config, screensNow);

            return result;
        }
        catch (Exception ex)
        {
            return (false, AppText.Format("VDD_Status_ReconfigureFailed", ex.Message));
        }
    }

    private static (bool ok, string message) ArrangeVirtualDisplay(string deviceName, VirtualScreenConfig config)
    {
        var currentMode = CreateDevMode();
        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref currentMode))
            return (false, AppText.Get("VDD_Status_ReadCurrentFailed"));

        var primaryBounds = Screen.PrimaryScreen?.Bounds
            ?? Screen.AllScreens.FirstOrDefault(s => s.Primary)?.Bounds
            ?? new Rectangle(0, 0, currentMode.dmPelsWidth, currentMode.dmPelsHeight);

        var isDuplicate = VirtualDisplayPlacementOptions.IsDuplicate(config.VirtualDisplayPlacement);

        // En modo duplicado se fuerza la resolución del monitor principal.
        var requestedWidth  = isDuplicate ? primaryBounds.Width  : config.Width;
        var requestedHeight = isDuplicate ? primaryBounds.Height : config.Height;

        var mode = TryGetBestSupportedMode(deviceName, requestedWidth, requestedHeight)
            ?? currentMode;

        config.Width  = mode.dmPelsWidth;
        config.Height = mode.dmPelsHeight;
        mode.dmPosition = GetVirtualDisplayPosition(primaryBounds, config.VirtualDisplayPlacement, config.Width, config.Height);
        mode.dmFields = DM_POSITION | DM_PELSWIDTH | DM_PELSHEIGHT;

        var result = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
        if (result != DISP_CHANGE_SUCCESSFUL)
        {
            var supportedModes = GetSupportedModes(deviceName);
            var supportedText = supportedModes.Count == 0
                ? string.Empty
                : AppText.Format("VDD_Status_SupportedModes", string.Join(", ", supportedModes.Select(m => $"{m.dmPelsWidth}x{m.dmPelsHeight}").Distinct()));
            var reason = result == DISP_CHANGE_BADMODE
                ? AppText.Get("VDD_Status_BadMode")
                : AppText.Format("VDD_Status_ApplyFailed", result);
            return (false, reason + supportedText);
        }

        result = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (result != DISP_CHANGE_SUCCESSFUL)
            return (false, AppText.Format("VDD_Status_RefreshFailed", result));

        if (isDuplicate)
            return (true, AppText.Format("VDD_Status_DuplicateOk", config.Width, config.Height));

        if (config.Width != requestedWidth || config.Height != requestedHeight)
        {
            return (true, AppText.Format("VDD_Status_ResolutionFallback",
                requestedWidth, requestedHeight,
                config.Width, config.Height,
                NormalizePlacementLabel(config.VirtualDisplayPlacement)));
        }

        return (true, AppText.Format("VDD_Status_Ok", NormalizePlacementLabel(config.VirtualDisplayPlacement), config.Width, config.Height));
    }

    private static List<DEVMODE> GetSupportedModes(string deviceName)
    {
        var modes = new List<DEVMODE>();

        for (var modeIndex = 0; ; modeIndex++)
        {
            var mode = CreateDevMode();
            if (!EnumDisplaySettings(deviceName, modeIndex, ref mode))
                break;

            if (modes.Any(existing => existing.dmPelsWidth == mode.dmPelsWidth && existing.dmPelsHeight == mode.dmPelsHeight))
                continue;

            modes.Add(mode);
        }

        return modes;
    }

    private static DEVMODE? TryGetBestSupportedMode(string deviceName, int requestedWidth, int requestedHeight)
    {
        var supportedModes = GetSupportedModes(deviceName);
        if (supportedModes.Count == 0)
            return null;

        var exactMode = supportedModes.FirstOrDefault(mode => mode.dmPelsWidth == requestedWidth && mode.dmPelsHeight == requestedHeight);
        if (exactMode.dmPelsWidth == requestedWidth && exactMode.dmPelsHeight == requestedHeight)
            return exactMode;

        return supportedModes
            .OrderBy(mode => Math.Abs(mode.dmPelsWidth - requestedWidth) + Math.Abs(mode.dmPelsHeight - requestedHeight))
            .ThenBy(mode => Math.Abs((mode.dmPelsWidth / (double)mode.dmPelsHeight) - (requestedWidth / (double)requestedHeight)))
            .First();
    }

    private void UpdateAppliedDisplayMetrics(VirtualScreenConfig config, Screen[] screensNow)
    {
        if (string.IsNullOrWhiteSpace(WindowsDeviceName))
            return;

        var index = Array.FindIndex(screensNow, s =>
            string.Equals(s.DeviceName, WindowsDeviceName, StringComparison.OrdinalIgnoreCase));
        WindowsMonitorIndex = index >= 0 ? index : null;
        if (WindowsMonitorIndex is int monitorIndex)
        {
            config.MonitorIndex = monitorIndex;
            config.Width = screensNow[monitorIndex].Bounds.Width;
            config.Height = screensNow[monitorIndex].Bounds.Height;
        }
    }

    private static POINTL GetVirtualDisplayPosition(Rectangle primaryBounds, string? placement, int width, int height)
    {
        var position = VirtualDisplayPlacementOptions.GetPosition(primaryBounds, placement, width, height);
        return new POINTL { x = position.X, y = position.Y };
    }

    private static string NormalizePlacementLabel(string? placement) =>
        AppText.Get(VirtualDisplayPlacementOptions.GetLocalizationKey(placement));

    /// <summary>
    /// Devuelve la resolución actual del monitor con el nombre de dispositivo dado,
    /// o null si no puede obtenerse.
    /// </summary>
    public static (int width, int height)? TryGetCurrentResolution(string deviceName)
    {
        var mode = CreateDevMode();
        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref mode))
            return null;
        if (mode.dmPelsWidth <= 0 || mode.dmPelsHeight <= 0)
            return null;
        return (mode.dmPelsWidth, mode.dmPelsHeight);
    }

    private static DEVMODE CreateDevMode() => new()
    {
        dmDeviceName = new string('\0', 32),
        dmFormName = new string('\0', 32),
        dmSize = (short)Marshal.SizeOf<DEVMODE>(),
    };

    public void Dispose()
    {
        StopKeepAlive();

        if (ParsecVddDriverApi.IsValidHandle(_handle) && _displayIdx >= 0)
        {
            try
            {
                ParsecVddDriverApi.RemoveDisplay(_handle, _displayIdx);
            }
            catch
            {
            }
        }

        Reset();
    }
}

