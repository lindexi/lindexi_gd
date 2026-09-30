using System.Diagnostics;
using System.Text.Json;

namespace XiaoXiIme.Cli;

internal sealed record SystemTestCommand(string Id, string FileName, IReadOnlyList<string> Arguments);

internal sealed record SystemTestCommandResult(string Id, int ExitCode, string StandardOutput, string StandardError);

internal static class SystemTestRunner
{
    public const string VmConfirmation = "I-UNDERSTAND-THIS-MODIFIES-WINDOWS";

    public static async Task<int> RunAsync(
        IReadOnlyList<SystemTestCommand> commands,
        string reportPath,
        bool allowDestructive,
        TextWriter output,
        TextWriter error)
    {
        if (!allowDestructive)
        {
            error.WriteLine($"System test execution is blocked. Pass --confirm {VmConfirmation} only inside a disposable VM.");
            return 6;
        }

        var results = new List<SystemTestCommandResult>();
        foreach (var command in commands)
        {
            output.WriteLine($"Running system test step: {command.Id}");
            var startInfo = new ProcessStartInfo(command.FileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var argument in command.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Unable to start {command.FileName}.");
            var standardOutput = await process.StandardOutput.ReadToEndAsync();
            var standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            results.Add(new SystemTestCommandResult(command.Id, process.ExitCode, standardOutput, standardError));
            if (process.ExitCode != 0)
            {
                break;
            }
        }

        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        var failed = results.FirstOrDefault(result => result.ExitCode != 0);
        if (failed is not null)
        {
            error.WriteLine($"System test step failed: {failed.Id}. Report: {reportPath}");
            return 7;
        }

        output.WriteLine($"System test commands passed. Report: {reportPath}");
        return 0;
    }
}
