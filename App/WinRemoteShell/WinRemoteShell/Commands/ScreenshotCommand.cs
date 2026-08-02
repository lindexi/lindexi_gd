using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;

namespace WinRemoteShell.Commands;

[Command("screenshot")]
internal sealed class ScreenshotCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Option("output")]
    public string? Output { get; init; }

    public async Task<int> RunAsync()
    {
        var outputPath = await ScreenshotClient.CaptureAsync(ServerAddressResolver.Resolve(Server), Output);
        Console.WriteLine(outputPath);
        return 0;
    }
}
