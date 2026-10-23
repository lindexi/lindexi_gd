using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Images;

/// <summary>
/// 为 Coding Agent 提供独立的图片分析子智能体工具。
/// </summary>
public sealed class CodingImageAnalysisToolSource : ICodingWorkspaceToolSource
{
    private readonly CopilotChatManager _chatManager;

    /// <summary>
    /// 创建图片分析工具源。
    /// </summary>
    /// <param name="chatManager">用于创建独立手动发送上下文的聊天管理器。</param>
    public CodingImageAnalysisToolSource(CopilotChatManager chatManager)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        _chatManager = chatManager;
    }

    /// <inheritdoc />
    public IReadOnlyList<AITool> CreateTools(string workspacePath) =>
        CreateToolRegistrations(workspacePath).Select(registration => registration.Tool).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ToolRegistration> CreateToolRegistrations(string workspacePath) =>
        new CodingImageAnalysisTools(_chatManager).AsToolRegistrations();
}
