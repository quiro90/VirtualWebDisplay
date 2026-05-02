using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Helper para detectar y obtener IPs locales de Windows asignadas mediante redes de anclaje USB (Tethering).
/// </summary>
public static class UsbNetworkHelper
{
    // Subredes típicas generadas por USB Tethering (192.168.42.x Android, 192.168.137.x iOS/Windows)
    private static readonly string[] UsbSubnets = { "192.168.42.", "192.168.137." };

    private static string? _cachedIp;
    private static DateTime _lastCheck = DateTime.MinValue;

    /// <summary>
    /// Busca y retorna la IP local de la PC en la interfaz generada por la red USB Tethering.
    /// </summary>
    public static string? GetUsbTetheringIp()
    {
        // Cache ligera de 2 segundos para evitar bloquear la UI con llamadas nativas repetitivas
        if ((DateTime.UtcNow - _lastCheck).TotalSeconds < 2)
            return _cachedIp;

        _lastCheck = DateTime.UtcNow;
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(i => i.OperationalStatus == OperationalStatus.Up);

        foreach (var iface in interfaces)
        {
            var properties = iface.GetIPProperties();
            foreach (var unicast in properties.UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    string ipString = unicast.Address.ToString();
                    if (UsbSubnets.Any(subnet => ipString.StartsWith(subnet)))
                    {
                        _cachedIp = ipString;
                        return _cachedIp; // Retorna la IP de la PC en esta subred
                    }
                }
            }
        }
        _cachedIp = null;
        return null;
    }

    /// <summary>
    /// Indica si hay una conexión física y activa de USB Tethering disponible.
    /// </summary>
    public static bool IsUsbTetheringAvailable() => GetUsbTetheringIp() != null;
}