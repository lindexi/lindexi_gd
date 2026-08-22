using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;

namespace WinRemoteShell.Commands;

[Command("exec")]
internal sealed class ExecCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Option("timeout")]
    public int? Timeout { get; init; }

    [Option("working-directory")]
    public string? WorkingDirectory { get; init; }

    [Value(0, int.MaxValue)]
    public IReadOnlyList<string> Arguments { get; init; } = [];

    public async Task<int> RunAsync()
    {
        await ExecClient.ExecuteAsync(
            ServerAddressResolver.Resolve(Server),
            Arguments,
            Timeout,
            Console.Out,
            WorkingDirectory);
        return 0;
    }
}
