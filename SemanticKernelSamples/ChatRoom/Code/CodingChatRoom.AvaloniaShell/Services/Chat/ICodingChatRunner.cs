using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentLib.Coding;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingChatRunner
{
    Task<ICodingAgentRunResult> RunAsync
    (
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken
    );

    Task<bool> TryCompactConversationAsync(AgentLib.Model.CopilotChatSession session,
        string? instructions, CancellationToken cancellationToken) => Task.FromResult(false);

    Task InjectMessageAsync
    (
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken
    )
        => throw new NotSupportedException();
}