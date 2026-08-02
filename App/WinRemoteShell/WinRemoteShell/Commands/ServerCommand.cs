using System.Net;
using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Server;

namespace WinRemoteShell.Commands;

[Command("server")]
internal sealed class ServerCommand : ICommandHandler
{
    [Option("port")]
    public int? Port { get; init; }

    [Option("install-service")]
    public bool InstallService { get; init; }

    [Option("uninstall-service")]
    public bool UninstallService { get; init; }

    public async Task<int> RunAsync()
    {
        if (InstallService && UninstallService)
        {
            throw new ArgumentException("Service installation and uninstallation cannot be requested together.");
        }

        if (InstallService)
        {
            var servicePort = Port ?? AvailablePortFinder.GetAvailablePort(IPAddress.Any);
            await WindowsServiceInstaller.InstallAsync(servicePort);
            Console.WriteLine($"Windows service installed and started on port {servicePort}.");
            return 0;
        }

        if (UninstallService)
        {
            await WindowsServiceInstaller.UninstallAsync();
            Console.WriteLine("Windows service stopped and uninstalled.");
            return 0;
        }

        var port = Port ?? AvailablePortFinder.GetAvailablePort(IPAddress.Any);
        Console.WriteLine(port);
        await ServerHost.RunAsync(port);
        return 0;
    }
}
