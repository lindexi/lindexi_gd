using System.Text.Json;
using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Commands;

[Command("ps")]
internal sealed class ProcessListCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Option("details")]
    public bool Details { get; init; }

    [Option("json")]
    public bool Json { get; init; }

    public async Task<int> RunAsync()
    {
        var response = await ProcessClient.ListAsync(ServerAddressResolver.Resolve(Server), Details);
        if (Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(response, AppJsonSerializerContext.Default.ProcessListResponse));
            return 0;
        }

        WriteTable(response.Processes);
        return 0;
    }

    private void WriteTable(IReadOnlyList<RemoteProcessInfo> processes)
    {
        if (!Details)
        {
            Console.WriteLine($"{"PID",8}  NAME");
            foreach (var process in processes)
            {
                Console.WriteLine($"{process.Id,8}  {process.Name}");
            }

            return;
        }

        Console.WriteLine($"{"PID",8}  {"WORKING SET",12}  {"PRIVATE",12}  {"THREADS",7}  {"STARTED (UTC)",-20}  NAME  PATH");
        foreach (var process in processes)
        {
            Console.WriteLine(
                $"{process.Id,8}  {FormatBytes(process.WorkingSetBytes),12}  {FormatBytes(process.PrivateMemoryBytes),12}  {process.ThreadCount?.ToString() ?? "-",7}  {process.StartTimeUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-",-20}  {process.Name}  {process.FilePath ?? "-"}");
        }
    }

    private static string FormatBytes(long? bytes) => bytes is null ? "-" : $"{bytes.Value / 1024d / 1024d:F1} MB";
}
