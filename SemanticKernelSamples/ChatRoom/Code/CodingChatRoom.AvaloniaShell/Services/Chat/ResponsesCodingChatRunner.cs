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
    public async Task<CodingAgentRunResult> RunAsync(IReadOnlyList<AIContent> contents,
        string? workspacePath, CodingChatRunOptions options, CancellationToken cancellationToken)
    {
        var session = chatManager.SelectedSession;
        Guid sessionId = session.SessionId;
        var conversation = session.ResponsesSession;
        var context = await chatManager.CreateManualSendMessageContextAsync(cancellationToken).ConfigureAwait(false);
        var run = await agent.RunAsync(context, contents, conversation, workspacePath,
            options.EnableDotNetRun, options.ReasoningEffort, cancellationToken).ConfigureAwait(false);
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
            await chatManager.ChatLogger.LogMessageAsync(sessionId, run.AssistantChatMessage).ConfigureAwait(false);
        }
    }
}
