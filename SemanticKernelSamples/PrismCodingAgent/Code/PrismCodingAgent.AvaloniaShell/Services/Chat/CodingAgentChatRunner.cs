using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace PrismCodingAgent.AvaloniaShell.Services.Chat;

internal sealed class CodingAgentChatRunner : ICodingChatRunner
{
    private readonly CopilotChatManager _chatManager;
    private readonly CodingAgent _codingAgent;
    private ICodingAgentRunResult? _activeRun;

    public CodingAgentChatRunner(CopilotChatManager chatManager, CodingAgent codingAgent)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(codingAgent);
        _chatManager = chatManager;
        _codingAgent = codingAgent;
    }

    public async Task<ICodingAgentRunResult> RunAsync
    (
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(contents);
        Guid sessionId = _chatManager.SelectedSession.SessionId;
        IManualSendMessageContext context = await _chatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);
        ICodingAgentRunResult run = await _codingAgent
            .RunAsync
            (
                context,
                contents,
                workspacePath,
                options,
                cancellationToken
            )
            .ConfigureAwait(false);
        _activeRun = run;
        return new CompletedCodingAgentRunResult
        (
            run.AssistantChatMessage,
            CompleteAndClearActiveRunAsync(run, sessionId)
        );
    }

    public async Task<bool> TryCompactConversationAsync(AgentLib.Model.CopilotChatSession session,
        string? instructions, CancellationToken cancellationToken)
    {
        if (session.AgentSession is null) return false;
        await _chatManager.ReduceSessionAsync(chatReducer: null, requestText: instructions,
            additionalPrompt: instructions, cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task InjectMessageAsync
    (
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(contents);
        ICodingAgentRunResult activeRun = _activeRun
                                         ?? throw new InvalidOperationException("当前没有正在运行的编程代理。");
        return activeRun.InjectMessageAsync(contents, cancellationToken);
    }

    private async Task<string?> CompleteAndClearActiveRunAsync(ICodingAgentRunResult run, Guid sessionId)
    {
        try
        {
            try
            {
                return await run.CompletionTask.ConfigureAwait(false);
            }
            finally
            {
                await _chatManager.ChatLogger
                    .LogMessageAsync(sessionId, run.AssistantChatMessage)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            if (ReferenceEquals(_activeRun, run))
            {
                _activeRun = null;
            }
        }
    }
}