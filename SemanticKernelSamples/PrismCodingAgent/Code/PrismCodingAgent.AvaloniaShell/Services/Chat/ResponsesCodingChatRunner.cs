using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace CodingAgent.AvaloniaShell.Services.Chat;

internal sealed class ResponsesCodingChatRunner(CopilotChatManager chatManager, ResponsesCodingAgent agent) : ICodingChatRunner
{
    public async Task<bool> TryCompactConversationAsync(AgentLib.Model.CopilotChatSession session,
        string? instructions, CancellationToken cancellationToken)
    {
        var context = await chatManager.CreateManualSendMessageContextAsync(cancellationToken).ConfigureAwait(false);
        return await agent.TryCompactConversationAsync(context.LanguageModel, session.ResponsesSession,
            instructions, cancellationToken).ConfigureAwait(false);
    }

    private ICodingAgentRunResult? _activeRun;
    private CopilotChatSession? _activeSession;

    public async Task InjectMessageAsync(IReadOnlyList<AIContent> contents, CancellationToken cancellationToken)
    {
        var run = _activeRun ?? throw new InvalidOperationException("当前没有正在运行的 Responses 代理。");
        await run.InjectMessageAsync(contents, cancellationToken).ConfigureAwait(false);

        var session = _activeSession ?? throw new InvalidOperationException("当前没有活动 Responses 会话。");
        var message = CopilotChatMessage.CreateUser(contents);
        await session.AddMessageAsync(message).ConfigureAwait(false);
        await chatManager.ChatLogger.LogMessageAsync(session.SessionId, message).ConfigureAwait(false);
    }

    public async Task<ICodingAgentRunResult> RunAsync(IReadOnlyList<AIContent> contents,
        string? workspacePath, CodingChatRunOptions options, CancellationToken cancellationToken)
    {
        var session = chatManager.SelectedSession;
        Guid sessionId = session.SessionId;
        var conversation = session.ResponsesSession;
        var context = await chatManager.CreateManualSendMessageContextAsync(cancellationToken).ConfigureAwait(false);
        var run = await agent.RunAsync(context, contents, conversation, workspacePath,
            options, cancellationToken).ConfigureAwait(false);
        _activeRun = run;
        _activeSession = session;
        return new CompletedCodingAgentRunResult(run.AssistantChatMessage, CompleteAsync(run, sessionId));
    }

    private async Task<string?> CompleteAsync(ICodingAgentRunResult run, Guid sessionId)
    {
        try
        {
            return await run.CompletionTask.ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await chatManager.ChatLogger.LogMessageAsync(sessionId, run.AssistantChatMessage).ConfigureAwait(false);
            }
            finally
            {
                _activeRun = null;
                _activeSession = null;
            }
        }
    }
}
