using System.Net;
using Microsoft.AspNetCore.Builder;
using WinRemoteShell.Server;

namespace WinRemoteShell.Tests;

internal sealed class TestServerHost : IAsyncDisposable
{
    private readonly WebApplication _application;

    private TestServerHost(WebApplication application, Uri address)
    {
        _application = application;
        Address = address;
    }

    public Uri Address { get; }

    public static async Task<TestServerHost> StartAsync()
    {
        var port = AvailablePortFinder.GetAvailablePort(IPAddress.Loopback);
        var application = ServerHost.Create(port);
        await application.StartAsync();
        return new TestServerHost(application, new Uri($"http://localhost:{port}/"));
    }

    public async ValueTask DisposeAsync()
    {
        await _application.StopAsync();
        await _application.DisposeAsync();
    }
}