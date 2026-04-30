using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Helpers;

/// <summary>
/// Centraliza la lógica de llamadas nativas (Win32) para permitir el arrastre
/// de formularios sin bordes desde controles personalizados (como barras de título).
/// </summary>
internal static class WindowDragHelper
{
    private const int WmNclButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    public static void EnableDrag(IntPtr formHandle, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();
        SendMessage(formHandle, WmNclButtonDown, HtCaption, 0);
    }
}