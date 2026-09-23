using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingChatRunner
{
    Task<CodingAgentRunResult> RunAsync
    (
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken
    );

    Task InjectMessageAsync
    (
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken
    )
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

    public async Task<CodingAgentRunResult> RunAsync
    (
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(contents);
        CopilotChatSession session = _chatManager.SelectedSession;
        Guid sessionId = session.SessionId;
        IManualSendMessageContext context = await _chatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);
        Task messagesAppendedTask = WaitForMessageAsync
        (
            session,
            context.AssistantChatMessage,
            cancellationToken
        );
        CodingAgentRunResult run = await _codingAgent
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
        Task completedTask = await Task.WhenAny(messagesAppendedTask, run.CompletionTask).ConfigureAwait(false);
        await completedTask.ConfigureAwait(false);
        return new CodingAgentRunResult
        (
            run.AssistantChatMessage,
            CompleteAndClearActiveRunAsync(run, sessionId)
        );
    }

    public Task InjectMessageAsync
    (
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(contents);
        CodingAgentRunResult activeRun = _activeRun
                                         ?? throw new InvalidOperationException("当前没有正在运行的编程代理。");
        return activeRun.InjectMessageAsync(contents, cancellationToken);
    }

    private async Task<string?> CompleteAndClearActiveRunAsync(CodingAgentRunResult run, Guid sessionId)
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

    private static async Task WaitForMessageAsync
    (
        CopilotChatSession session,
        CopilotChatMessage message,
        CancellationToken cancellationToken
    )
    {
        if (session.ChatMessages.Contains(message))
        {
            return;
        }

        var appended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems?.Contains(message) == true)
            {
                appended.TrySetResult();
            }
        }

        session.ChatMessages.CollectionChanged += OnCollectionChanged;
        try
        {
            if (session.ChatMessages.Contains(message))
            {
                return;
            }

            await appended.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            session.ChatMessages.CollectionChanged -= OnCollectionChanged;
        }
    }
}