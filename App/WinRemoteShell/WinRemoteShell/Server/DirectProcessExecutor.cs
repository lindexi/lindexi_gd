using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace WinRemoteShell.Server;

internal sealed class DirectProcessExecutor
{
    public async IAsyncEnumerable<string> ExecuteAsync(
        IReadOnlyList<string> arguments,
        string workingDirectory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (arguments.Count == 0 || string.IsNullOrWhiteSpace(arguments[0]))
        {
            throw new ArgumentException("An executable file name is required.", nameof(arguments));
        }

        using var process = StartProcess(arguments, workingDirectory);

        var output = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        using var executionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var cancellationRegistration = executionCancellationSource.Token.Register(() => KillProcess(process));
        var completionTask = CompleteOutputAsync(process, output.Writer, executionCancellationSource.Token);

        try
        {
            await foreach (var line in output.Reader.ReadAllAsync(executionCancellationSource.Token))
            {
                yield return line;
            }

            await completionTask;
        }
        finally
        {
            await executionCancellationSource.CancelAsync();
            KillProcess(process);

            try
            {
                await completionTask;
            }
            catch (OperationCanceledException) when (executionCancellationSource.IsCancellationRequested)
            {
            }
        }
    }

    private static Process StartProcess(IReadOnlyList<string> arguments, string workingDirectory)
    {
        Win32Exception? lastException = null;
        foreach (var searchPath in GetExecutableSearchPaths(workingDirectory))
        {
            var process = CreateProcess(Path.Combine(searchPath, arguments[0]), arguments, workingDirectory);
            try
            {
                process.Start();
                return process;
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode is 2 or 3)
            {
                process.Dispose();
                lastException = exception;
            }
        }

        throw lastException ?? new Win32Exception(2);
    }

    private static IEnumerable<string> GetExecutableSearchPaths(string workingDirectory)
    {
        yield return workingDirectory;

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        foreach (var searchPath in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return searchPath;
        }
    }

    private static Process CreateProcess(
        string executablePath,
        IReadOnlyList<string> arguments,
        string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        for (var index = 1; index < arguments.Count; index++)
        {
            startInfo.ArgumentList.Add(arguments[index]);
        }

        return new Process { StartInfo = startInfo };
    }

    private static async Task CompleteOutputAsync(
        Process process,
        ChannelWriter<string> output,
        CancellationToken cancellationToken)
    {
        try
        {
            var standardOutputTask = CopyLinesAsync(process.StandardOutput, output, cancellationToken);
            var standardErrorTask = CopyLinesAsync(process.StandardError, output, cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            output.TryComplete();
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException)
        {
            KillProcess(process);
            output.TryComplete(exception);
        }
    }

    private static async Task CopyLinesAsync(
        StreamReader reader,
        ChannelWriter<string> output,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            await output.WriteAsync(line, cancellationToken);
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
