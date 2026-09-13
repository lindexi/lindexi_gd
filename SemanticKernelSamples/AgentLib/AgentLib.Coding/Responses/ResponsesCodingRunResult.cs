using AgentLib.Model;

namespace AgentLib.Coding.Responses;

/// <summary>
/// 表示一次 Responses 编程代理运行。
/// </summary>
public sealed record ResponsesCodingRunResult(
    CopilotChatMessage AssistantChatMessage,
    Task<string?> CompletionTask);
