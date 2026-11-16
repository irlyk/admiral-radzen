using System.Net;
using System.Net.Sockets;

namespace Admiral.Playwright.Infrastructure.Hosting;

internal static class NetworkPorts
{
    public static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
