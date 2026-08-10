using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace AgentLib.Coding.Sandboxes;

internal sealed class WinRemoteShellProcessRunner : IWinRemoteShellRunner
{
    private readonly string _winRemoteShellPath;
    private readonly string _serverAddress;

    internal WinRemoteShellProcessRunner(string winRemoteShellPath, string serverAddress)
    {
        _winRemoteShellPath = winRemoteShellPath;
        _serverAddress = serverAddress;
    }

    public async Task PushAsync(string sourcePath, string remoteTargetPath, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(
            ["push", "--server", _serverAddress, "--source", sourcePath, "--target", remoteTargetPath],
            cancellationToken).ConfigureAwait(false);
        EnsureSuccess("推送文件", result);
    }

    public async Task<string> ExecuteAsync(string command, int timeoutSeconds, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(
            ["exec", "--server", _serverAddress, "--timeout", timeoutSeconds.ToString(CultureInfo.InvariantCulture), "--", command],
            cancellationToken).ConfigureAwait(false);
        EnsureSuccess("执行远端命令", result);
        return JoinOutput(result);
    }

    public async Task PullAsync(string remoteSourcePath, string localOutputPath, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(
            ["pull", "--server", _serverAddress, "--source", remoteSourcePath, "--output", localOutputPath],
            cancellationToken).ConfigureAwait(false);
        EnsureSuccess("拉取文件", result);
    }

    private async Task<ProcessResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _winRemoteShellPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 WinRemoteShell 客户端。");
        }

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            await standardOutputTask.ConfigureAwait(false),
            await standardErrorTask.ConfigureAwait(false));
    }

    private static void EnsureSuccess(string operation, ProcessResult result)
    {
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"WinRemoteShell {operation}失败，客户端退出码为 {result.ExitCode}。{JoinOutput(result)}");
        }
    }

    private static string JoinOutput(ProcessResult result) =>
        string.Join(Environment.NewLine, new[] { result.StandardOutput, result.StandardError }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
            .TrimEnd();

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}