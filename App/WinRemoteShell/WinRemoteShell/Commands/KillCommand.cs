using System.Text.Json;
using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Commands;

[Command("kill")]
internal sealed class KillCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Option("pid")]
    public int? ProcessId { get; init; }

    [Option("name")]
    public string? ProcessName { get; init; }

    [Option("tree")]
    public bool KillTree { get; init; }

    [Option("json")]
    public bool Json { get; init; }

    public async Task<int> RunAsync()
    {
        var hasProcessId = ProcessId is not null;
        var hasProcessName = !string.IsNullOrWhiteSpace(ProcessName);
        if (hasProcessId == hasProcessName)
        {
            Console.Error.WriteLine("Specify exactly one target: --pid <id> or --name <name>.");
            return 2;
        }

        if (ProcessId <= 0)
        {
            Console.Error.WriteLine("--pid must be greater than zero.");
            return 2;
        }

        var response = await KillClient.KillAsync(
            ServerAddressResolver.Resolve(Server),
            ProcessId,
            ProcessName,
            KillTree);

        if (Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(response, AppJsonSerializerContext.Default.KillProcessesResponse));
        }
        else if (response.Processes.Count == 0)
        {
            Console.WriteLine("No matching process was found.");
        }
        else
        {
            foreach (var process in response.Processes)
            {
                var result = process.Killed ? "killed" : $"failed: {process.Error}";
                Console.WriteLine($"{process.Id} {process.Name}: {result}");
            }
        }

        return response.Processes.All(process => process.Killed) ? 0 : 1;
    }
}
