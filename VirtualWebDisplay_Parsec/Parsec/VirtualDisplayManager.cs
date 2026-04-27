using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Parsec;

/// <summary>
/// Manages a Parsec Virtual Display by talking directly to the installed driver.
/// No external DLL is required next to this executable.
/// </summary>
public sealed class VirtualDisplayManager : IDisposable
{
    public const string InstallUrl = "https://parsec.app/downloads";

    private const string AdapterGuid = "{00b41627-04c4-429e-a26e-0265cf50c8fa}";

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

    public bool IsActive { get; private set; }
    public int? WindowsMonitorIndex { get; private set; }
    public string? WindowsDeviceName { get; private set; }

    public static (bool ok, string message) VerifyDriverAvailability()
    {
        if (!DriverApi.OpenHandle(AdapterGuid, out var handle))
        {
            return (false, AppText.Get("Parsec_Driver_NotFound"));
        }

        DriverApi.CloseHandle(handle);
        return (true, AppText.Get("Parsec_Driver_Detected"));
    }

    public (bool ok, string message) TryCreate(VirtualScreenConfig config)
    {
        try
        {
            var screensBefore = Screen.AllScreens;
            var screenNamesBefore = screensBefore
                .Select(s => s.DeviceName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!DriverApi.OpenHandle(AdapterGuid, out _handle))
                return VerifyDriverAvailability();

            if (!DriverApi.AddDisplay(_handle, out _displayIdx))
            {
                Reset();
                return (false, AppText.Get("Parsec_Driver_CreateDisplayFailed"));
            }

            StartKeepAlive();

            Screen? virtualScreen = null;
            for (var i = 0; i < 50; i++)
            {
                Thread.Sleep(100);

                var screensNow = Screen.AllScreens;
                virtualScreen = screensNow.FirstOrDefault(s => !screenNamesBefore.Contains(s.DeviceName));
                if (virtualScreen is not null)
                    break;
            }

            if (virtualScreen is null && Screen.AllScreens.Length > screensBefore.Length)
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
                arrangeStatus = "Windows creó el monitor virtual, pero no se pudo identificar su pantalla automáticamente.";
            }

            IsActive = true;
            return (true,
                $"Parsec VDD: monitor virtual listo (índice interno {_displayIdx}, {Screen.AllScreens.Length} monitores totales" +
                (WindowsMonitorIndex is int idx ? $", MonitorIndex Windows {idx}" : string.Empty) +
                "). " + arrangeStatus);
        }
        catch (Exception ex)
        {
            Reset();
            return (false, $"Parsec VDD: error inesperado - {ex.Message}");
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
                    if (DriverApi.IsValidHandle(_handle))
                        DriverApi.Update(_handle);

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

        if (DriverApi.IsValidHandle(_handle))
            DriverApi.CloseHandle(_handle);

        _handle = IntPtr.Zero;
        _displayIdx = -1;
        WindowsMonitorIndex = null;
        WindowsDeviceName = null;
        IsActive = false;
    }

    public (bool ok, string message) TryReconfigure(VirtualScreenConfig config)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(WindowsDeviceName))
            return (false, "El monitor virtual todavía no está listo para reconfigurarse.");

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
            return (false, $"No se pudo reconfigurar el monitor virtual: {ex.Message}");
        }
    }

    private static (bool ok, string message) ArrangeVirtualDisplay(string deviceName, VirtualScreenConfig config)
    {
        var currentMode = CreateDevMode();
        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref currentMode))
            return (false, "No se pudo leer la configuración actual del monitor virtual; queda con la disposición que asigne Windows.");

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
                : $" Modos soportados: {string.Join(", ", supportedModes.Select(m => $"{m.dmPelsWidth}x{m.dmPelsHeight}").Distinct())}.";
            var reason = result == DISP_CHANGE_BADMODE
                ? "La resolución pedida no está soportada por el driver del monitor virtual."
                : $"Windows creó el monitor virtual pero no permitió aplicarle posición/resolución (código {result}).";
            return (false, reason + supportedText);
        }

        result = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (result != DISP_CHANGE_SUCCESSFUL)
            return (false, $"Windows guardó cambios parciales del monitor virtual, pero no pudo refrescar la topología (código {result}).");

        if (isDuplicate)
            return (true, $"Monitor virtual en modo duplicado (clone) del monitor principal con resolución {config.Width}x{config.Height}.");

        if (config.Width != requestedWidth || config.Height != requestedHeight)
        {
            return (true,
                $"La resolución solicitada {requestedWidth}x{requestedHeight} no estaba disponible. " +
                $"Se aplicó la más cercana soportada: {config.Width}x{config.Height}, ubicada a la {NormalizePlacementLabel(config.VirtualDisplayPlacement)} del monitor principal.");
        }

        return (true, $"Ubicado a la {NormalizePlacementLabel(config.VirtualDisplayPlacement)} del monitor principal con resolución {config.Width}x{config.Height}.");
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
        VirtualDisplayPlacementOptions.GetDisplayLabel(placement);

    private static DEVMODE CreateDevMode() => new()
    {
        dmDeviceName = new string('\0', 32),
        dmFormName = new string('\0', 32),
        dmSize = (short)Marshal.SizeOf<DEVMODE>(),
    };

    public void Dispose()
    {
        StopKeepAlive();

        if (DriverApi.IsValidHandle(_handle) && _displayIdx >= 0)
        {
            try
            {
                DriverApi.RemoveDisplay(_handle, _displayIdx);
            }
            catch
            {
            }
        }

        Reset();
    }

    private static unsafe class DriverApi
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x1;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
        private const uint DIGCF_PRESENT = 0x2;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;

        private enum IoCtlCode
        {
            Add = 0x22E004,
            Remove = 0x22A008,
            Update = 0x22A00C,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OVERLAPPED
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public IntPtr Pointer;
            public IntPtr hEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA_A
        {
            public int cbSize;
            public char DevicePath;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileA(
            char* lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            void* lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            void* hTemplateFile);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandleNative(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeviceIoControl(
            IntPtr device,
            uint code,
            void* lpInBuffer,
            int nInBufferSize,
            void* lpOutBuffer,
            int nOutBufferSize,
            void* lpBytesReturned,
            ref OVERLAPPED lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOverlappedResultEx(
            IntPtr handle,
            ref OVERLAPPED lpOverlapped,
            out uint lpNumberOfBytesTransferred,
            int dwMilliseconds,
            [MarshalAs(UnmanagedType.Bool)] bool bAlertable);

        [DllImport("kernel32.dll", EntryPoint = "CreateEventW", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(
            void* lpEventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bManualReset,
            [MarshalAs(UnmanagedType.Bool)] bool bInitialState,
            string? lpName);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevsA(
            ref Guid classGuid,
            void* enumerator,
            void* hwndParent,
            uint flags);

        [DllImport("setupapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet,
            SP_DEVINFO_DATA* deviceInfoData,
            ref Guid interfaceClassGuid,
            uint memberIndex,
            SP_DEVICE_INTERFACE_DATA* deviceInterfaceData);

        [DllImport("setupapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetailA(
            IntPtr deviceInfoSet,
            SP_DEVICE_INTERFACE_DATA* deviceInterfaceData,
            void* deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize,
            int* requiredSize,
            SP_DEVINFO_DATA* deviceInfoData);

        [DllImport("setupapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        public static bool OpenHandle(string guid, out IntPtr handle)
        {
            handle = IntPtr.Zero;

            var interfaceGuid = Guid.Parse(guid);
            var devInfo = SetupDiGetClassDevsA(ref interfaceGuid, null, null, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            if (!IsValidHandle(devInfo))
                return false;

            try
            {
                var devInterface = new SP_DEVICE_INTERFACE_DATA
                {
                    cbSize = sizeof(SP_DEVICE_INTERFACE_DATA),
                };

                for (uint i = 0; SetupDiEnumDeviceInterfaces(devInfo, null, ref interfaceGuid, i, &devInterface); ++i)
                {
                    int detailSize = 0;
                    SetupDiGetDeviceInterfaceDetailA(devInfo, &devInterface, null, 0, &detailSize, null);

                    var detail = (SP_DEVICE_INTERFACE_DETAIL_DATA_A*)Marshal.AllocHGlobal(detailSize);
                    try
                    {
                        detail->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_A);

                        if (!SetupDiGetDeviceInterfaceDetailA(devInfo, &devInterface, detail, detailSize, &detailSize, null))
                            continue;

                        handle = CreateFileA(
                            &detail->DevicePath,
                            GENERIC_READ | GENERIC_WRITE,
                            FILE_SHARE_READ | FILE_SHARE_WRITE,
                            null,
                            OPEN_EXISTING,
                            FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING | FILE_FLAG_OVERLAPPED | FILE_FLAG_WRITE_THROUGH,
                            null);

                        if (IsValidHandle(handle))
                        {
                            Update(handle);
                            return true;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal((IntPtr)detail);
                    }
                }

                return false;
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(devInfo);
            }
        }

        public static void CloseHandle(IntPtr handle)
        {
            if (IsValidHandle(handle))
                CloseHandleNative(handle);
        }

        public static bool AddDisplay(IntPtr handle, out int index)
        {
            if (IoControl(handle, IoCtlCode.Add, null, out index, 5000))
            {
                Update(handle);
                return true;
            }

            return false;
        }

        public static bool RemoveDisplay(IntPtr handle, int index)
        {
            var input = new byte[2];
            input[1] = (byte)(index & 0xFF);

            if (IoControl(handle, IoCtlCode.Remove, input, 1000))
            {
                Update(handle);
                return true;
            }

            return false;
        }

        public static void Update(IntPtr handle)
        {
            IoControl(handle, IoCtlCode.Update, null, 1000);
        }

        public static bool IsValidHandle(IntPtr handle) =>
            handle != IntPtr.Zero && handle != new IntPtr(-1);

        private static bool IoControl(IntPtr handle, IoCtlCode code, byte[]? input, int timeout)
        {
            return IoControl(handle, code, input, null, timeout);
        }

        private static bool IoControl(IntPtr handle, IoCtlCode code, byte[]? input, out int result, int timeout)
        {
            int output;
            var success = IoControl(handle, code, input, &output, timeout);
            result = output;
            return success;
        }

        private static bool IoControl(IntPtr handle, IoCtlCode code, byte[]? input, int* result, int timeout)
        {
            var inBuffer = new byte[32];
            if (input is { Length: > 0 })
                Array.Copy(input, inBuffer, Math.Min(input.Length, inBuffer.Length));

            var overlapped = new OVERLAPPED();

            fixed (byte* buffer = inBuffer)
            {
                var outputLength = result is null ? 0 : sizeof(int);
                overlapped.hEvent = CreateEvent(null, false, false, null);

                try
                {
                    var sent = DeviceIoControl(
                        handle,
                        (uint)code,
                        buffer,
                        inBuffer.Length,
                        result,
                        outputLength,
                        null,
                        ref overlapped);

                    if (!sent && Marshal.GetLastWin32Error() == 0x6)
                        return false;

                    return GetOverlappedResultEx(handle, ref overlapped, out _, timeout, false);
                }
                finally
                {
                    if (overlapped.hEvent != IntPtr.Zero)
                        CloseHandleNative(overlapped.hEvent);
                }
            }
        }
    }
}

