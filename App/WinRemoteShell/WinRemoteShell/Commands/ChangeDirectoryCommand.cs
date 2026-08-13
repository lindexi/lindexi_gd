using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;

namespace WinRemoteShell.Commands;

[Command("cd")]
internal sealed class ChangeDirectoryCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Value(0)]
    public required string Path { get; init; }

    public async Task<int> RunAsync()
    {
        var result = await ChangeDirectoryClient.ChangeAsync(ServerAddressResolver.Resolve(Server), Path);
        Console.WriteLine(result.Path);
        return 0;
    }
}
