using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Sandboxes;

/// <summary>
/// 使用 WinRemoteShell 为 Coding Agent 提供 Windows 沙盒执行工具。
/// </summary>
public sealed class WindowsSandboxToolSource : ICodingWorkspaceToolSource
{
    private readonly IWinRemoteShellRunner? _fixedRunner;
    private WindowsSandboxConfiguration _configuration;

    /// <summary>
    /// 创建启用的 Windows 沙盒工具源。
    /// </summary>
    /// <param name="winRemoteShellPath">WinRemoteShell 客户端可执行文件路径或命令名。</param>
    /// <param name="serverAddress">WinRemoteShell Server 地址。</param>
    public WindowsSandboxToolSource(string winRemoteShellPath, string serverAddress)
        : this(true, winRemoteShellPath, serverAddress)
    {
    }

    /// <summary>
    /// 使用当前沙盒配置创建工具源。
    /// </summary>
    /// <param name="isEnabled">是否启用沙盒工具。</param>
    /// <param name="winRemoteShellPath">WinRemoteShell 客户端可执行文件路径或命令名。</param>
    /// <param name="serverAddress">WinRemoteShell Server 地址。</param>
    public WindowsSandboxToolSource(
        bool isEnabled,
        string winRemoteShellPath,
        string serverAddress)
    {
        _configuration = CreateConfiguration(isEnabled, winRemoteShellPath, serverAddress);
    }

    internal WindowsSandboxToolSource(IWinRemoteShellRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        _fixedRunner = runner;
        _configuration = new WindowsSandboxConfiguration(true, string.Empty, string.Empty);
    }

    /// <summary>
    /// 更新下一次编程运行使用的沙盒配置。
    /// </summary>
    /// <param name="isEnabled">是否启用沙盒工具。</param>
    /// <param name="winRemoteShellPath">WinRemoteShell 客户端可执行文件路径或命令名。</param>
    /// <param name="serverAddress">WinRemoteShell Server 地址。</param>
    public void UpdateConfiguration(
        bool isEnabled,
        string winRemoteShellPath,
        string serverAddress)
    {
        if (_fixedRunner is not null)
        {
            throw new InvalidOperationException("使用固定 Runner 创建的沙盒工具源不支持更新配置。");
        }

        Volatile.Write(
            ref _configuration,
            CreateConfiguration(isEnabled, winRemoteShellPath, serverAddress));
    }

    /// <inheritdoc />
    public IReadOnlyList<AITool> CreateTools(string workspacePath) =>
        CreateToolRegistrations(workspacePath).Select(registration => registration.Tool).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ToolRegistration> CreateToolRegistrations(string workspacePath)
    {
        WindowsSandboxConfiguration configuration = Volatile.Read(ref _configuration);
        if (!configuration.IsEnabled)
        {
            return [];
        }

        IWinRemoteShellRunner runner = _fixedRunner
            ?? new WinRemoteShellProcessRunner(configuration.ToolPath, configuration.ServerAddress);
        return new WindowsSandboxTools(workspacePath, runner).AsToolRegistrations();
    }

    private static WindowsSandboxConfiguration CreateConfiguration(
        bool isEnabled,
        string winRemoteShellPath,
        string serverAddress)
    {
        if (isEnabled && string.IsNullOrWhiteSpace(winRemoteShellPath))
        {
            throw new ArgumentException("WinRemoteShell 客户端路径不能为空。", nameof(winRemoteShellPath));
        }
        if (isEnabled && string.IsNullOrWhiteSpace(serverAddress))
        {
            throw new ArgumentException("WinRemoteShell Server 地址不能为空。", nameof(serverAddress));
        }

        return new WindowsSandboxConfiguration(
            isEnabled,
            winRemoteShellPath?.Trim() ?? string.Empty,
            serverAddress?.Trim() ?? string.Empty);
    }

    private sealed record WindowsSandboxConfiguration(
        bool IsEnabled,
        string ToolPath,
        string ServerAddress);
}
