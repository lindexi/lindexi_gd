using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface IWindowsSandboxConnectionTester
{
    Task<WindowsSandboxConnectionTestResult> TestAsync
    (
        string toolPath,
        string serverAddress,
        CancellationToken cancellationToken = default
    );
}

internal sealed class WindowsSandboxConnectionTester : IWindowsSandboxConnectionTester
{
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromSeconds(15);

    public async Task<WindowsSandboxConnectionTestResult> TestAsync
    (
        string toolPath,
        string serverAddress,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(toolPath))
        {
            throw new ArgumentException("沙箱工具路径不能为空。", nameof(toolPath));
        }

        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            throw new ArgumentException("沙箱连接地址不能为空。", nameof(serverAddress));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = toolPath.Trim(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add("ls");
        startInfo.ArgumentList.Add("--server");
        startInfo.ArgumentList.Add(serverAddress);

        using var process = new Process();
        process.StartInfo = startInfo;
        try
        {
            if (!process.Start())
            {
                return new WindowsSandboxConnectionTestResult(false, "沙箱连接测试失败：无法启动 WinRemoteShell 客户端。");
            }
        }
        catch (Win32Exception exception)
        {
            return new WindowsSandboxConnectionTestResult(false, $"沙箱连接测试失败：{exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            return new WindowsSandboxConnectionTestResult(false, $"沙箱连接测试失败：{exception.Message}");
        }

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationTokenSource.CancelAfter(s_testTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillProcess(process);
            return new WindowsSandboxConnectionTestResult(false, "沙箱连接测试失败：连接测试超时。");
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);
            throw;
        }

        string standardOutput = await standardOutputTask.ConfigureAwait(false);
        string standardError = await standardErrorTask.ConfigureAwait(false);
        if (process.ExitCode == 0)
        {
            return new WindowsSandboxConnectionTestResult(true, "沙箱连接测试成功。");
        }

        string detail = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
        detail = detail.Trim();
        string suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
        return new WindowsSandboxConnectionTestResult
        (
            false,
            $"沙箱连接测试失败：WinRemoteShell 退出码为 {process.ExitCode}。{suffix}"
        );
    }

    private static void KillProcess(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
}

internal sealed record WindowsSandboxConnectionTestResult(bool IsSuccessful, string Message);