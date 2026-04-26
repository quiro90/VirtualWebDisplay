using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VirtualWebDisplay.Infrastructure;

using System.Net.NetworkInformation;
using System.Net.Sockets;

public static class NetworkAddressHelper
{
    public static string DetectLocalIp() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .FirstOrDefault() ?? "127.0.0.1";

    public static string BuildAccessUrl(string host, int port) =>
        port == 80 ? $"http://{host}/" : $"http://{host}:{port}/";
}

