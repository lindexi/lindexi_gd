using System.Net;
using System.Net.Sockets;
using WinRemoteShell.Server;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace WinRemoteShell.Tests;

[TestClass]
public sealed class AvailablePortFinderTests
{
    [TestMethod]
    public void WhenPortIsSelectedThenItCanBeBoundAgain()
    {
        var port = AvailablePortFinder.GetAvailablePort(IPAddress.Loopback);
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        socket.Bind(new IPEndPoint(IPAddress.Loopback, port));

        Assert.AreEqual(port, ((IPEndPoint)socket.LocalEndPoint!).Port);
    }
}