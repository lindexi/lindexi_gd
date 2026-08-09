using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingChatRunner
{
    Task<CodingAgentRunResult> RunAsync(
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CancellationToken cancellationToken);

    Task InjectMessageAsync(
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

internal sealed class CodingAgentChatRunner : ICodingChatRunner
{
    private readonly CopilotChatManager _chatManager;
    private readonly CodingAgent _codingAgent;
    private CodingAgentRunResult? _activeRun;

    public CodingAgentChatRunner(CopilotChatManager chatManager, CodingAgent codingAgent)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(codingAgent);
        _chatManager = chatManager;
        _codingAgent = codingAgent;
    }

    public async Task<CodingAgentRunResult> RunAsync(
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);
        IManualSendMessageContext context = await _chatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);
        CodingAgentRunResult run = await _codingAgent
            .RunAsync(context, contents, workspacePath, cancellationToken)
            .ConfigureAwait(false);
        _activeRun = run;
        return new CodingAgentRunResult(
            run.AssistantChatMessage,
            CompleteAndClearActiveRunAsync(run));
    }

    public Task InjectMessageAsync(
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);
        CodingAgentRunResult activeRun = _activeRun
            ?? throw new InvalidOperationException("当前没有正在运行的编程代理。");
        return activeRun.InjectMessageAsync(contents, cancellationToken);
    }

    private async Task<string?> CompleteAndClearActiveRunAsync(CodingAgentRunResult run)
    {
        try
        {
            return await run.CompletionTask.ConfigureAwait(false);
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
