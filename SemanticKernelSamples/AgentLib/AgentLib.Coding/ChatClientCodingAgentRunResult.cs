using AgentLib.Model;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001

namespace AgentLib.Coding;

internal sealed class ChatClientCodingAgentRunResult : ICodingAgentRunResult
{
    private readonly MessageInjectingChatClient _messageInjector;
    private readonly AgentSession _session;

    internal ChatClientCodingAgentRunResult(CopilotChatMessage assistantChatMessage,
        Task<string?> completionTask, MessageInjectingChatClient messageInjector, AgentSession session)
    {
        AssistantChatMessage = assistantChatMessage;
        CompletionTask = completionTask;
        _messageInjector = messageInjector;
        _session = session;
    }

    public CopilotChatMessage AssistantChatMessage { get; }
    public Task<string?> CompletionTask { get; }

    public Task InjectMessageAsync(IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);
        if (contents.Count == 0)
        {
            throw new ArgumentException("消息内容不能为空。", nameof(contents));
        }
        return _messageInjector.EnqueueMessagesAsync(_session,
            [new ChatMessage(ChatRole.User, [.. contents])], cancellationToken);
    }
}
