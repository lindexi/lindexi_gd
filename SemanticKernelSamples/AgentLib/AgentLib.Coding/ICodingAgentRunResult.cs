using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>定义一次编程代理活动运行的协议无关契约。</summary>
public interface ICodingAgentRunResult
{
    /// <summary>获取可观察流式更新的助手消息。</summary>
    CopilotChatMessage AssistantChatMessage { get; }

    /// <summary>获取表示运行和收尾完成的任务。</summary>
    Task<string?> CompletionTask { get; }

    /// <summary>向活动运行提交有序用户内容。</summary>
    Task InjectMessageAsync(IReadOnlyList<AIContent> contents, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("当前运行不支持消息注入。");
}
