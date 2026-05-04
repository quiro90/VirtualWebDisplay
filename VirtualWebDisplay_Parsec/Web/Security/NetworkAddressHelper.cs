using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VirtualWebDisplay.Web.Security;

using System.Net.NetworkInformation;
using System.Net.Sockets;

public static class NetworkAddressHelper
{
    public static string DetectLocalIp()
    {
        // Tipos de interfaz que corresponden a VPN/túneles virtuales — se excluyen
        // para evitar mostrar IPs de VPN corporativas o adaptadores virtuales.
        var excludedTypes = new[]
        {
            NetworkInterfaceType.Tunnel,
            NetworkInterfaceType.Ppp,
        };

        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && !excludedTypes.Contains(n.NetworkInterfaceType))
            .Select(n => new
            {
                Interface  = n,
                Properties = n.GetIPProperties(),
            })
            .ToList();

        // Preferir interfaces con gateway definido (Ethernet/WiFi físico con ruta real)
        var withGateway = candidates
            .Where(c => c.Properties.GatewayAddresses.Any(g =>
                g.Address.AddressFamily == AddressFamily.InterNetwork))
            .SelectMany(c => c.Properties.UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .FirstOrDefault();

        if (withGateway != null)
            return withGateway;

        // Fallback: cualquier interfaz activa no-loopback no-túnel
        return candidates
            .SelectMany(c => c.Properties.UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .FirstOrDefault() ?? "127.0.0.1";
    }

    public static string BuildAccessUrl(string host, int port) =>
        port == 80 ? $"http://{host}/" : $"http://{host}:{port}/";
}

