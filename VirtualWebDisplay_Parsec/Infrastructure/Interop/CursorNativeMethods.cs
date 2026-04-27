using System.Runtime.InteropServices;

namespace VirtualWebDisplay.Infrastructure.Interop;

/// <summary>
/// Declaraciones P/Invoke para las APIs Win32 usadas en la captura del cursor.
/// Centraliza todo el interop nativo, manteniendo el código de negocio libre de dependencias Win32.
/// </summary>
internal static class CursorNativeMethods
{
    internal const int CursorShowing = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CURSORINFO
    {
        public int    cbSize;
        public int    flags;
        public IntPtr hCursor;
        public POINT  ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ICONINFO
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool   fIcon;
        public int    xHotspot;
        public int    yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll")]
    internal static extern bool GetCursorInfo(out CURSORINFO pci);

    [DllImport("user32.dll")]
    internal static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    internal static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

    [DllImport("user32.dll")]
    internal static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);
}
