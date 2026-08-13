using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;

namespace WinRemoteShell.Commands;

[Command("ls")]
internal sealed class ListCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    public async Task<int> RunAsync()
    {
        var listing = await ListClient.ListAsync(ServerAddressResolver.Resolve(Server));
        foreach (var entry in listing.Entries)
        {
            Console.WriteLine(entry.Name);
        }

        return 0;
    }
}
