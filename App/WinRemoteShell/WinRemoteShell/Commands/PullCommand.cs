using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;

namespace WinRemoteShell.Commands;

[Command("pull")]
internal sealed class PullCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Option("source")]
    public required string Source { get; init; }

    [Option("output")]
    public required string Output { get; init; }

    public async Task<int> RunAsync()
    {
        await PullClient.PullAsync(ServerAddressResolver.Resolve(Server), Source, Output);
        return 0;
    }
}
