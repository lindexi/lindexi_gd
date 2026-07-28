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
}

internal sealed class CodingAgentChatRunner : ICodingChatRunner
{
    private readonly CopilotChatManager _chatManager;
    private readonly CodingAgent _codingAgent;

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
        return await _codingAgent
            .RunAsync(context, contents, workspacePath, cancellationToken)
            .ConfigureAwait(false);
    }
}