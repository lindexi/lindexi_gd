using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001

namespace AgentLib.Coding;

/// <summary>
/// 表示一次编程代理的活动运行。
/// </summary>
public sealed record CodingAgentRunResult
{
    private readonly MessageInjectingChatClient? _messageInjector;
    private readonly AgentSession? _session;

    /// <summary>
    /// 创建不支持消息注入的运行结果。
    /// </summary>
    /// <param name="assistantChatMessage">可直接绑定以观察流式更新的助手消息。</param>
    /// <param name="completionTask">等待运行、历史补全和工作区租约释放全部完成的任务。</param>
    public CodingAgentRunResult(CopilotChatMessage assistantChatMessage, Task<string?> completionTask)
    {
        ArgumentNullException.ThrowIfNull(assistantChatMessage);
        ArgumentNullException.ThrowIfNull(completionTask);
        AssistantChatMessage = assistantChatMessage;
        CompletionTask = completionTask;
    }

    internal CodingAgentRunResult(
        CopilotChatMessage assistantChatMessage,
        Task<string?> completionTask,
        MessageInjectingChatClient messageInjector,
        AgentSession session)
        : this(assistantChatMessage, completionTask)
    {
        ArgumentNullException.ThrowIfNull(messageInjector);
        ArgumentNullException.ThrowIfNull(session);
        _messageInjector = messageInjector;
        _session = session;
    }

    /// <summary>
    /// 获取可直接绑定以观察流式更新的助手消息。
    /// </summary>
    public CopilotChatMessage AssistantChatMessage { get; }

    /// <summary>
    /// 获取等待运行、历史补全和工作区租约释放全部完成的任务。
    /// </summary>
    public Task<string?> CompletionTask { get; }

    /// <summary>
    /// 向当前活动运行注入一条用户消息。
    /// </summary>
    /// <param name="contents">用户消息内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task InjectMessageAsync(
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);
        if (contents.Count == 0)
        {
            throw new ArgumentException("消息内容不能为空。", nameof(contents));
        }

        MessageInjectingChatClient messageInjector = _messageInjector
            ?? throw new InvalidOperationException("当前运行不支持消息注入。");
        return messageInjector.EnqueueMessagesAsync(
            _session!,
            [new ChatMessage(ChatRole.User, [.. contents])],
            cancellationToken);
    }
}
