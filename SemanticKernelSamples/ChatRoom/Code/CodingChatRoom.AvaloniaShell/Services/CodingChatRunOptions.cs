using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.Coding;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal readonly record struct CodingChatRunOptions(
    bool EnableAutomaticCompression,
    bool EnableDotNetRun,
    ReasoningEffort? ReasoningEffort)
{
    public static CodingChatRunOptions Default { get; } = new(
        EnableAutomaticCompression: true,
        EnableDotNetRun: false,
        ReasoningEffort: null);
}

internal static class CodingAgentRunExtensions
{
    public static Task<CodingAgentRunResult> RunAsync(
        this CodingAgent codingAgent,
        IManualSendMessageContext context,
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codingAgent);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);

        if (options.ReasoningEffort is not null)
        {
            context = new ConfiguredManualSendMessageContext(context, options.ReasoningEffort.Value);
        }

        return codingAgent.RunAsync(
            context,
            contents,
            workspacePath,
            options.EnableAutomaticCompression,
            options.EnableDotNetRun,
            cancellationToken);
    }

    private sealed class ConfiguredManualSendMessageContext(
        IManualSendMessageContext inner,
        ReasoningEffort reasoningEffort) : IManualSendMessageContext
    {
        public CopilotChatMessage UserChatMessage => inner.UserChatMessage;

        public CopilotChatMessage AssistantChatMessage => inner.AssistantChatMessage;

        public IChatClient ChatClient => inner.ChatClient;

        public AgentLib.IMainThreadDispatcher MainThreadDispatcher => inner.MainThreadDispatcher;

        public IReadOnlyList<AITool> DefaultTools => inner.DefaultTools;

        public Task<ChatClientAgent> GetChatClientAgentAsync(
            Action<ChatClientAgentOptions>? configure = null,
            CancellationToken cancellationToken = default)
        {
            return inner.GetChatClientAgentAsync(
                agentOptions =>
                {
                    configure?.Invoke(agentOptions);
                    ChatOptions chatOptions = agentOptions.ChatOptions?.Clone() ?? new ChatOptions();
                    chatOptions.Reasoning = new ReasoningOptions { Effort = reasoningEffort };
                    agentOptions.ChatOptions = chatOptions;
                },
                cancellationToken);
        }

        public Task<AgentSession> GetAgentSessionAsync(CancellationToken cancellationToken = default) =>
            inner.GetAgentSessionAsync(cancellationToken);

        public void AppendResponseUpdate(AgentResponseUpdate update) => inner.AppendResponseUpdate(update);

        public Task AppendMessagesToSessionAsync() => inner.AppendMessagesToSessionAsync();

        public IDisposable StartChatting() => inner.StartChatting();
    }
}
