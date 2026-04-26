using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// Manages a Virtual Display Driver (VirtualDrivers/VDD) monitor.
/// The driver is installed separately and monitors are always present —
/// no runtime creation or removal, only detection and positioning.
/// </summary>
public sealed class VirtualDisplayManager : IDisposable
{
    public const string InstallUrl = "https://github.com/VirtualDrivers/Virtual-Display-Driver/releases";

    private const string VddAdapterKeyword = "Virtual Display Driver";

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]  public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

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
    private const uint CDS_UPDATEREGISTRY = 0x00000001;
    private const uint CDS_NORESET = 0x10000000;
    private const int DISP_CHANGE_SUCCESSFUL = 0;

    private readonly int _instanceIndex;

    public bool IsActive { get; private set; }
    public int? WindowsMonitorIndex { get; private set; }
    public string? WindowsDeviceName { get; private set; }

    public VirtualDisplayManager(int instanceIndex = 0)
    {
        _instanceIndex = instanceIndex;
    }

    public static (bool ok, string message) VerifyDriverAvailability()
    {
        var adapters = GetVddAdapters().ToList();
        if (adapters.Count == 0)
        {
            return (false,
                "No se encontro Virtual Display Driver instalado o ningun monitor virtual activo.\n" +
                "Instala desde GitHub Releases o ejecuta:\n" +
                "  winget install VirtualDrivers.Virtual-Display-Driver");
        }

        return (true, $"Virtual Display Driver detectado ({adapters.Count} monitor(es) virtual(es)).");
    }

    public (bool ok, string message) TryCreate(VirtualScreenConfig config)
    {
        try
        {
            var adapters = GetVddAdapters().ToList();
            if (adapters.Count == 0)
                return VerifyDriverAvailability();

            if (_instanceIndex >= adapters.Count)
            {
                return (false,
                    $"Se necesita el monitor virtual #{_instanceIndex + 1}, pero Virtual Display Driver " +
                    $"solo tiene {adapters.Count} monitor(es) activo(s).\n" +
                    "Configura mas monitores en C:\\VirtualDisplayDriver\\vdd_settings.xml.");
            }

            var adapter = adapters[_instanceIndex];
            WindowsDeviceName = adapter.DeviceName;
            UpdateMonitorMetrics(config, Screen.AllScreens);

            if (!VirtualDisplayPlacementOptions.IsDuplicate(config.VirtualDisplayPlacement))
            {
                ArrangeVirtualDisplay(adapter.DeviceName, config);
                Thread.Sleep(250);
                UpdateMonitorMetrics(config, Screen.AllScreens);
            }

            IsActive = true;
            return (true,
                $"Virtual Display Driver: monitor virtual listo " +
                $"(indice {_instanceIndex}, {Screen.AllScreens.Length} monitores totales" +
                (WindowsMonitorIndex is int idx ? $", MonitorIndex Windows {idx}" : string.Empty) +
                $", {config.Width}x{config.Height}).");
        }
        catch (Exception ex)
        {
            return (false, $"Virtual Display Driver: error inesperado - {ex.Message}");
        }
    }

    public (bool ok, string message) TryReconfigure(VirtualScreenConfig config)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(WindowsDeviceName))
            return (false, "El monitor virtual todavia no esta listo para reconfigurarse.");

        try
        {
            ArrangeVirtualDisplay(WindowsDeviceName, config);
            Thread.Sleep(250);
            UpdateMonitorMetrics(config, Screen.AllScreens);
            return (true, "Posicion actualizada.");
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo reconfigurar el monitor virtual: {ex.Message}");
        }
    }

    private void UpdateMonitorMetrics(VirtualScreenConfig config, Screen[] screens)
    {
        if (string.IsNullOrWhiteSpace(WindowsDeviceName))
            return;

        var index = Array.FindIndex(screens, s =>
            string.Equals(s.DeviceName, WindowsDeviceName, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            WindowsMonitorIndex = index;
            config.MonitorIndex = index;
            config.Width = screens[index].Bounds.Width;
            config.Height = screens[index].Bounds.Height;
        }
    }

    private static void ArrangeVirtualDisplay(string deviceName, VirtualScreenConfig config)
    {
        var currentMode = CreateDevMode();
        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref currentMode))
            return;

        var primaryBounds = Screen.PrimaryScreen?.Bounds
            ?? Screen.AllScreens.FirstOrDefault(s => s.Primary)?.Bounds
            ?? new Rectangle(0, 0, currentMode.dmPelsWidth, currentMode.dmPelsHeight);

        var mode = currentMode;
        mode.dmPosition = GetVirtualDisplayPosition(
            primaryBounds, config.VirtualDisplayPlacement, currentMode.dmPelsWidth, currentMode.dmPelsHeight);
        mode.dmFields = DM_POSITION;

        var result = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_NORESET, IntPtr.Zero);
        if (result == DISP_CHANGE_SUCCESSFUL)
            ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
    }

    private static POINTL GetVirtualDisplayPosition(Rectangle primaryBounds, string? placement, int width, int height)
    {
        var position = VirtualDisplayPlacementOptions.GetPosition(primaryBounds, placement, width, height);
        return new POINTL { x = position.X, y = position.Y };
    }

    private static IEnumerable<DISPLAY_DEVICE> GetVddAdapters()
    {
        for (uint i = 0; ; i++)
        {
            var device = CreateDisplayDevice();
            if (!EnumDisplayDevices(null, i, ref device, 0))
                break;

            if (device.DeviceString.Contains(VddAdapterKeyword, StringComparison.OrdinalIgnoreCase))
                yield return device;
        }
    }

    private static DISPLAY_DEVICE CreateDisplayDevice() => new()
    {
        cb = Marshal.SizeOf<DISPLAY_DEVICE>(),
        DeviceName   = new string('\0', 32),
        DeviceString = new string('\0', 128),
        DeviceID     = new string('\0', 128),
        DeviceKey    = new string('\0', 128),
    };

    private static DEVMODE CreateDevMode() => new()
    {
        dmDeviceName = new string('\0', 32),
        dmFormName   = new string('\0', 32),
        dmSize = (short)Marshal.SizeOf<DEVMODE>(),
    };

    public void Dispose()
    {
        // VDD monitors are persistent — no removal needed.
        IsActive = false;
        WindowsMonitorIndex = null;
        WindowsDeviceName = null;
    }
}
