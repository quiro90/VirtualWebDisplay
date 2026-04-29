using System.Runtime.InteropServices;

namespace VirtualWebDisplay.Parsec;

/// <summary>
/// API de bajo nivel para comunicarse con el driver Parsec Virtual Display Driver (VDD).
/// Encapsula llamadas P/Invoke a setupapi.dll y kernel32.dll para gestionar dispositivos virtuales.
/// Compartido entre VirtualDisplayManager y ParsecVddDriverVerifier.
/// </summary>
internal static unsafe class ParsecVddDriverApi
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

    internal enum IoCtlCode
    {
        Add = 0x22E004,
        Remove = 0x22A008,
        Update = 0x22A00C,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OVERLAPPED
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
