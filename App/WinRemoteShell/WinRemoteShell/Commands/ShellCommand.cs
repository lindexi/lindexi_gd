using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;

namespace WinRemoteShell.Commands;

[Command("shell")]
internal sealed class ShellCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    public async Task<int> RunAsync()
    {
        await ShellClient.RunAsync(ServerAddressResolver.Resolve(Server), Console.In, Console.Out);
        return 0;
    }
}
