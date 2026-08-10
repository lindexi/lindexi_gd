using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Sandboxes;

/// <summary>
/// 使用 WinRemoteShell 为 Coding Agent 提供 Windows 沙盒执行工具。
/// </summary>
public sealed class WindowsSandboxToolSource : ICodingWorkspaceToolSource
{
    private readonly IWinRemoteShellRunner _runner;

    /// <summary>
    /// 创建 Windows 沙盒工具源。
    /// </summary>
    /// <param name="winRemoteShellPath">WinRemoteShell 客户端可执行文件路径或命令名。</param>
    /// <param name="serverAddress">WinRemoteShell Server 地址。</param>
    public WindowsSandboxToolSource(string winRemoteShellPath, string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(winRemoteShellPath))
        {
            throw new ArgumentException("WinRemoteShell 客户端路径不能为空。", nameof(winRemoteShellPath));
        }
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            throw new ArgumentException("WinRemoteShell Server 地址不能为空。", nameof(serverAddress));
        }

        _runner = new WinRemoteShellProcessRunner(winRemoteShellPath, serverAddress);
    }

    internal WindowsSandboxToolSource(IWinRemoteShellRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        _runner = runner;
    }

    /// <inheritdoc />
    public IReadOnlyList<AITool> CreateTools(string workspacePath) =>
        new WindowsSandboxTools(workspacePath, _runner).AsAITools();
}
