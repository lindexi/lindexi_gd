using System.Net;
using System.Net.Sockets;

namespace HiwelairchawCekaywebeale;

static class TestHostBuilder
{
    public static TestHost GetTestHost(Action<WebApplication> configure)
    {
        var port = GetAvailablePort();
        var builder = WebApplication.CreateBuilder();
        var host = $"http://127.0.0.1:{port}/";
        builder.WebHost.UseUrls(host);
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        var app = builder.Build();

        configure(app);

        _ = app.RunAsync();

        return new TestHost(host, app);
    }

    public static int GetAvailablePort()
    {
        return GetAvailablePort(IPAddress.Loopback);
    }

    /// <summary>
    /// 获取一个可用端口
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public static int GetAvailablePort(IPAddress ip)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(ip, 0));
        socket.Listen(1);
        var ipEndPoint = (IPEndPoint) socket.LocalEndPoint!;
        var port = ipEndPoint.Port;
        return port;
    }
}