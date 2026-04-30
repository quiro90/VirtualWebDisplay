using System;
using System.Windows.Forms;

namespace VirtualWebDisplay.UI.Helpers;

/// <summary>
/// Centraliza la lógica de marshaling segura para el hilo de la interfaz de usuario (UI).
/// Elimina la duplicación del patrón InvokeRequired/BeginInvoke a lo largo de la aplicación.
/// </summary>
internal static class UiDispatcherHelper
{
    public static void InvokeSafely(this Control? control, Action action)
    {
        if (control is null || control.IsDisposed || !control.IsHandleCreated)
            return;

        try
        {
            if (control.InvokeRequired)
                control.BeginInvoke(action);
            else
                action();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // El control fue destruido entre la validación y la ejecución.
            // Es un escenario seguro de ignorar en el ciclo de cierre de WinForms (race condition esperada).
        }
    }
}