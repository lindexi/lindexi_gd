using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentLib.Coding;
using AgentLib.Coding.Responses;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class ResponsesCodingChatRunner(
    ResponsesCodingAgent agent,
    Func<CopilotChatSession> getSession,
    Func<string> getModel) : ICodingChatRunner
{
    private ResponsesCodingRunResult? _activeRun;
    private readonly Queue<IReadOnlyList<AIContent>> _pendingMessages = new();

    public async Task<CodingAgentRunResult> RunAsync(
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken)
    {
        ResponsesCodingRunResult run = await agent.RunAsync(
            getSession(),
            contents,
            getModel(),
            workspacePath,
            new ResponsesCodingRunOptions
            {
                EnableDotNetRun = options.EnableDotNetRun,
                ReasoningEffort = options.ReasoningEffort,
            },
            cancellationToken).ConfigureAwait(false);
        _activeRun = run;
        return new CodingAgentRunResult(
            run.AssistantChatMessage,
            CompleteAsync(run, workspacePath, options, cancellationToken));
    }

    public Task InjectMessageAsync(
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingMessages.Enqueue(contents);
        return Task.CompletedTask;
    }

    public Task CancelAsync(CancellationToken cancellationToken = default) => agent.CancelAsync(cancellationToken);

    public Task<bool> StopLanguageServerAsync() => agent.StopLanguageServerAsync();

    public Task CompressAsync(CopilotChatSession session, CancellationToken cancellationToken = default) =>
        agent.CompactAsync(session, getModel(), cancellationToken);

    private async Task<string?> CompleteAsync(
        ResponsesCodingRunResult run,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            string? result = await run.CompletionTask.ConfigureAwait(false);
            while (_pendingMessages.TryDequeue(out IReadOnlyList<AIContent>? contents))
            {
                ResponsesCodingRunResult next = await agent.RunAsync(
                    getSession(),
                    contents,
                    getModel(),
                    workspacePath,
                    new ResponsesCodingRunOptions
                    {
                        EnableDotNetRun = options.EnableDotNetRun,
                        ReasoningEffort = options.ReasoningEffort,
                    },
                    cancellationToken).ConfigureAwait(false);
                result = await next.CompletionTask.ConfigureAwait(false);
            }

            return result;
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
