using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Commands;

[Command("push")]
internal sealed class PushCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Option("source")]
    public required string Source { get; init; }

    [Option("target")]
    public required string Target { get; init; }

    [Option("mode")]
    public PushMode Mode { get; init; } = PushMode.Merge;

    public async Task<int> RunAsync()
    {
        await PushClient.PushAsync(ServerAddressResolver.Resolve(Server), Source, Target, Mode);
        return 0;
    }
}
