using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

internal static class CodingChatTestHost
{
    internal static Task<ICodingAgentRunResult> RunAsync(this CodingAgent agent,
        IManualSendMessageContext host, string prompt, string? workspacePath,
        bool enableAutomaticCompression = true, bool enableDotNetRun = false,
        CancellationToken cancellationToken = default) =>
        RunAsync(agent, host, [new TextContent(prompt)], workspacePath,
            enableAutomaticCompression, enableDotNetRun, cancellationToken);

    internal static async Task<ICodingAgentRunResult> RunAsync(this CodingAgent agent,
        IManualSendMessageContext host, IReadOnlyList<AIContent> contents, string? workspacePath,
        bool enableAutomaticCompression = true, bool enableDotNetRun = false,
        CancellationToken cancellationToken = default)
    {
        AIContent[] snapshot = [.. contents];
        var context = new CodingRunContext(host.ChatClient, host.AssistantChatMessage,
            await host.GetAgentSessionAsync(cancellationToken), host.MainThreadDispatcher);
        using IDisposable chatting = host.StartChatting();
        host.UserChatMessage.AppendContents(snapshot);
        await host.AppendMessagesToSessionAsync();
        return await agent.RunAsync(context, snapshot, workspacePath,
            enableAutomaticCompression, enableDotNetRun, cancellationToken);
    }
}
