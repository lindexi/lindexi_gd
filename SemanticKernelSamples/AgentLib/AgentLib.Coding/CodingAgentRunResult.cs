using AgentLib.Model;

namespace AgentLib.Coding;

/// <summary>
/// 表示一次编程代理运行的即时流式消息和完整生命周期任务。
/// </summary>
/// <param name="AssistantChatMessage">可直接绑定以观察流式更新的助手消息。</param>
/// <param name="CompletionTask">等待运行、历史补全和工作区租约释放全部完成的任务。</param>
public sealed record CodingAgentRunResult(
    CopilotChatMessage AssistantChatMessage,
    Task<string?> CompletionTask);
