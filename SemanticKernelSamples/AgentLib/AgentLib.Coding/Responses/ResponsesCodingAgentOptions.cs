using AgentLib;

namespace AgentLib.Coding.Responses;

/// <summary>
/// 定义 Responses 编程代理的创建选项。
/// </summary>
public sealed record ResponsesCodingAgentOptions
{
    /// <summary>
    /// 获取 Roslyn Language Server 启动命令。
    /// </summary>
    public string LanguageServerCommand { get; init; } = "roslyn-language-server";

    /// <summary>
    /// 获取由宿主提供的附加工作区工具源。
    /// </summary>
    public IReadOnlyList<ICodingWorkspaceToolSource> AdditionalToolSources { get; init; } = [];

    /// <summary>
    /// 获取要追加到系统提示词的 Copilot 指令文件路径。
    /// </summary>
    public string? CopilotInstructionsPath { get; init; }

    /// <summary>
    /// 获取用于更新界面消息的主线程调度器。
    /// </summary>
    public IMainThreadDispatcher? MainThreadDispatcher { get; init; }
}
