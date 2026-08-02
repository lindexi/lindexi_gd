using System.Net;
using System.Net.Sockets;

namespace WinRemoteShell.Server;

public static class AvailablePortFinder
{
    /// <summary>
    /// Finds an available TCP port for the specified IP address.
    /// </summary>
    public static int GetAvailablePort(IPAddress ip)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(ip, 0));
        socket.Listen(1);
        var ipEndPoint = (IPEndPoint)socket.LocalEndPoint!;
        var port = ipEndPoint.Port;
        return port;
    }
}
