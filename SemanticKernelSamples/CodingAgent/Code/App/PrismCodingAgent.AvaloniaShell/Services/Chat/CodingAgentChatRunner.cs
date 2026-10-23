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
        CopilotChatSession session = _chatManager.SelectedSession;
        AIContent[] runContents = System.Linq.Enumerable.ToArray(contents);
        var client = await _chatManager.AgentApiEndpointManager.PrimaryModel.GetChatClientAsync()
            .ConfigureAwait(false);
        var userMessage = new CopilotChatMessage(ChatRole.User, runContents);
        var assistantMessage = CopilotChatMessage.CreateAssistant(
            CopilotChatMessage.PlaceholderContent, isPresetInfo: false);
        var context = new CodingRunContext(client, assistantMessage, session.AgentSession,
            _chatManager.MainThreadDispatcher);
        IDisposable chatting = _chatManager.StartChatting();
        try
        {
            ICodingAgentRunResult run = await _codingAgent.RunAsync(
                context, runContents, workspacePath, options, cancellationToken).ConfigureAwait(false);
            session.SetAgentSession(context.AgentSession
                ?? throw new InvalidOperationException("编程代理未创建模型会话。"));
            cancellationToken.ThrowIfCancellationRequested();
            await session.AddMessageAsync(userMessage).ConfigureAwait(false);
            await _chatManager.ChatLogger.LogMessageAsync(session.SessionId, userMessage).ConfigureAwait(false);
            await session.AddMessageAsync(assistantMessage).ConfigureAwait(false);
            _activeRun = run;
            return new CompletedCodingAgentRunResult(run.AssistantChatMessage,
                CompleteAndClearActiveRunAsync(run, session.SessionId, chatting));
        }
        catch
        {
            chatting.Dispose();
            throw;
        }
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

    private async Task<string?> CompleteAndClearActiveRunAsync(ICodingAgentRunResult run, Guid sessionId,
        IDisposable chatting)
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
            chatting.Dispose();
            if (ReferenceEquals(_activeRun, run))
            {
                _activeRun = null;
            }
        }
    }
}