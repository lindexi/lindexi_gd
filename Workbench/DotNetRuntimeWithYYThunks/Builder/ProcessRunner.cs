using System.Diagnostics;
using System.Text.Json;

namespace Builder;

internal sealed record Command(string Executable, string[] Arguments, string WorkingDirectory);

internal static class ProcessRunner
{
    internal static async Task<int> RunAsync(Command command, string log, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(command.Executable)
        {
            WorkingDirectory = command.WorkingDirectory, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        foreach (string arg in command.Arguments) info.ArgumentList.Add(arg);
        await using var file = new StreamWriter(log, append: false);
        await file.WriteLineAsync(JsonSerializer.Serialize(command)).ConfigureAwait(false);
        using var gate = new SemaphoreSlim(1);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);
        using var process = new Process { StartInfo = info };
        cancellationToken.ThrowIfCancellationRequested();
        process.Start();
        async Task PumpAsync(StreamReader reader, string channel)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    await file.WriteLineAsync($"{DateTimeOffset.UtcNow:O} [{channel}] {line}").ConfigureAwait(false);
                    await file.FlushAsync().ConfigureAwait(false);
                    Console.WriteLine(line);
                }
                finally { gate.Release(); }
            }
        }
        Task output = PumpAsync(process.StandardOutput, "stdout"), error = PumpAsync(process.StandardError, "stderr");
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            await Task.WhenAll(output, error).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(output, error).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) throw;
            throw new TimeoutException(Text.Get("Timeout"));
        }
    }
}
