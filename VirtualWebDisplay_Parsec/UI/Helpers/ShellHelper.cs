using System;
using System.Diagnostics;

namespace VirtualWebDisplay.UI.Helpers;

/// <summary>
/// Centraliza la ejecución segura de procesos del sistema y la apertura de URLs.
/// </summary>
internal static class ShellHelper
{
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Se ignora de forma segura: previene crasheos si el SO no tiene una aplicación predeterminada asignada
        }
    }
}