using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Model;
using AgentLib;

using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingChatRunner
{
    Task<CodingAgentRunResult> RunAsync(
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken);

    Task InjectMessageAsync(
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    Task CancelAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    Task<bool> StopLanguageServerAsync() => Task.FromResult(false);

    Task CompressAsync(CopilotChatSession session, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
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
        CodingChatRunOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);
        CopilotChatSession session = _chatManager.SelectedSession;
        await SynchronizeResponsesMessagesAsync(session, cancellationToken).ConfigureAwait(false);
        IManualSendMessageContext context = await _chatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);
        CodingAgentRunResult run = await _codingAgent
            .RunAsync(
                context,
                contents,
                workspacePath,
                options,
                cancellationToken)
            .ConfigureAwait(false);
        session.ChatMessageCount = session.ChatMessages.Count;
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

    public Task<bool> StopLanguageServerAsync() => _codingAgent.StopLanguageServerAsync();

    public Task CompressAsync(CopilotChatSession session, CancellationToken cancellationToken = default) =>
        _chatManager.ReduceSessionAsync();

    private static Task SynchronizeResponsesMessagesAsync(
        CopilotChatSession session,
        CancellationToken cancellationToken)
    {
        if (session.AgentSession is null || session.ChatMessageCount >= session.ChatMessages.Count)
        {
            return Task.CompletedTask;
        }

        var messages = new List<ChatMessage>();
        foreach (CopilotChatMessage message in session.ChatMessages.Skip(session.ChatMessageCount))
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<AIContent> contents = message.MessageItems.SelectMany(ToAIContents).ToList();
            if (contents.Count > 0)
            {
                messages.Add(new ChatMessage(message.Role, contents));
            }
        }

        session.AppendAgentSessionMessages(messages);
        session.ChatMessageCount = session.ChatMessages.Count;
        return Task.CompletedTask;
    }

    private static IEnumerable<AIContent> ToAIContents(ICopilotChatMessageItem item)
    {
        switch (item)
        {
            case CopilotChatTextItem text:
                yield return new TextContent(text.Text);
                break;
            case CopilotChatReasoningItem reasoning:
                yield return new TextReasoningContent(reasoning.Text);
                break;
            case CopilotChatImageItem image:
                yield return new DataContent(image.Data.ToMemory(), image.MimeType);
                break;
            case CopilotChatToolItem tool:
                yield return new TextContent($"工具 {tool.ToolName} 输入：{tool.InputText}\n输出：{tool.OutputText}");
                break;
        }
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
