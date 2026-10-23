using AgentLib.Model;

namespace AgentLib.Coding;

/// <summary>承载助手消息和完成任务，不提供消息注入能力。</summary>
public sealed class CompletedCodingAgentRunResult : ICodingAgentRunResult
{
    /// <summary>创建消息和完成任务的结果。</summary>
    public CompletedCodingAgentRunResult(CopilotChatMessage assistantChatMessage, Task<string?> completionTask)
    {
        ArgumentNullException.ThrowIfNull(assistantChatMessage);
        ArgumentNullException.ThrowIfNull(completionTask);
        AssistantChatMessage = assistantChatMessage;
        CompletionTask = completionTask;
    }

    /// <inheritdoc />
    public CopilotChatMessage AssistantChatMessage { get; }

    /// <inheritdoc />
    public Task<string?> CompletionTask { get; }
}
