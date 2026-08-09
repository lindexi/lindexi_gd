using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
#pragma warning disable MAAI001

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingChatRunner
{
    Task<CodingAgentRunResult> RunAsync(IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CancellationToken cancellationToken);

    Task InjectMessageAsync(IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

internal sealed class CodingAgentChatRunner : ICodingChatRunner
{
    private readonly CopilotChatManager _chatManager;
    private readonly CodingAgent _codingAgent;
    private MessageInjectingChatClient? _messageInjector;

    public CodingAgentChatRunner(CopilotChatManager chatManager, CodingAgent codingAgent)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(codingAgent);
        _chatManager = chatManager;
        _codingAgent = codingAgent;
    }

    public async Task<CodingAgentRunResult> RunAsync(IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);
        IManualSendMessageContext context = await _chatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);
        ChatClientAgent agent = await context.GetChatClientAgentAsync(
            static options =>
            {
                options.EnableMessageInjection = true;
                options.RequirePerServiceCallChatHistoryPersistence = true;
            },
            cancellationToken).ConfigureAwait(false);
        _messageInjector = agent.GetService<MessageInjectingChatClient>()
            ?? throw new InvalidOperationException("当前 Agent 未启用消息注入。");
        return await _codingAgent
            .RunAsync(context, contents, workspacePath, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task InjectMessageAsync(IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);
        AgentSession session = _chatManager.SelectedSession.AgentSession
            ?? throw new InvalidOperationException("当前会话没有活动的 Agent Session。");
        MessageInjectingChatClient injector = _messageInjector
            ?? throw new InvalidOperationException("当前 Agent 未启用消息注入。");
        injector.EnqueueMessages(session,
            [new ChatMessage(ChatRole.User, [.. contents])]);
    }
}