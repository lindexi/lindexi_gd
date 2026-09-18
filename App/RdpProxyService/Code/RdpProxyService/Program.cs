using Microsoft.Extensions.Hosting.WindowsServices;
using RdpProxyService;

if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("RdpProxyService 仅支持 Windows。");
}

if (!WindowsServiceHelpers.IsWindowsService())
{
    await ServiceInstaller.InstallAndStartAsync();
    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = ServiceInstaller.ServiceName);
builder.Services.AddHostedService<RdpProxyWorker>();

await builder.Build().RunAsync();