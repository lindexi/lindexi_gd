using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class ResponsesCodingChatRunner(CopilotChatManager chatManager, ResponsesCodingAgent agent) : ICodingChatRunner
{
    private CodingAgentRunResult? _activeRun;

    public async Task InjectMessageAsync(IReadOnlyList<AIContent> contents, CancellationToken cancellationToken)
    {
        var run = _activeRun ?? throw new InvalidOperationException("当前没有正在运行的 Responses 代理。");
        await run.InjectMessageAsync(contents, cancellationToken).ConfigureAwait(false);
        await chatManager.AppendMessageAsync(AgentLib.Model.CopilotChatMessage.CreateUser(contents), cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodingAgentRunResult> RunAsync(IReadOnlyList<AIContent> contents,
        string? workspacePath, CodingChatRunOptions options, CancellationToken cancellationToken)
    {
        var session = chatManager.SelectedSession;
        Guid sessionId = session.SessionId;
        var conversation = session.ResponsesSession;
        var context = await chatManager.CreateManualSendMessageContextAsync(cancellationToken).ConfigureAwait(false);
        var run = await agent.RunAsync(context, contents, conversation, workspacePath,
            options, cancellationToken).ConfigureAwait(false);
        _activeRun = run;
        return new CodingAgentRunResult(run.AssistantChatMessage, CompleteAsync(run, sessionId));
    }

    private async Task<string?> CompleteAsync(CodingAgentRunResult run, Guid sessionId)
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
            }
        }
    }
}
