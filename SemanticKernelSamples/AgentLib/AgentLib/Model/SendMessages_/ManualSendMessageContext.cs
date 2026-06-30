using AgentLib;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable MAAI001

namespace AgentLib.Model;

/// <summary>
/// <see cref="IManualSendMessageContext"/> 的实现，提供延迟创建的 <see cref="ChatClientAgent"/> 和 <see cref="AgentSession"/>。
/// </summary>
internal sealed class ManualSendMessageContext : IManualSendMessageContext
{
    private ChatClientAgent? _chatClientAgent;
    private AgentSession? _agentSession;

    private bool _isFirstUpdate = true;

    /// <summary>
    /// 关联的 <see cref="CopilotChatManager"/> 实例。
    /// </summary>
    public required CopilotChatManager ChatManager { get; init; }

    /// <summary>
    /// AI 上下文提供者集合。
    /// </summary>
    public required IReadOnlyList<AIContextProvider>? AIContextProviders { get; init; }

    /// <summary>
    /// 当前聊天会话。
    /// </summary>
    public required CopilotChatSession Session { get; init; }

    /// <summary>
    /// 默认工具列表。
    /// </summary>
    public required IReadOnlyList<AITool> DefaultTools { get; init; }

    /// <inheritdoc />
    public required CopilotChatMessage UserChatMessage { get; init; }

    /// <inheritdoc />
    public required CopilotChatMessage AssistantChatMessage { get; init; }

    /// <inheritdoc />
    public required IChatClient ChatClient { get; init; }

    /// <inheritdoc />
    public IMainThreadDispatcher? MainThreadDispatcher => ChatManager.MainThreadDispatcher;

    /// <inheritdoc />
    public async Task<ChatClientAgent> GetChatClientAgentAsync(CancellationToken cancellationToken = default)
    {
        if (_chatClientAgent is not null)
        {
            return _chatClientAgent;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 压缩器始终使用 PrimaryModel 获取 IChatClient，与聊天逻辑无关
        IChatClient reducerChatClient = await ChatManager.AgentApiEndpointManager.PrimaryModel.GetChatClientAsync().ConfigureAwait(false);

        var chatClientAgentOptions = new ChatClientAgentOptions()
        {
            ChatOptions = new ChatOptions()
            {
                Tools = [.. DefaultTools],
                Reasoning = new ReasoningOptions()
                {
                    Effort = ReasoningEffort.None,
                }
            },
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
            {
                ChatReducer = new CopilotChatManagerToolCallChatReducer(reducerChatClient)
            }),
            RequirePerServiceCallChatHistoryPersistence = true,
        };

        if (AIContextProviders is { Count: > 0 })
        {
            chatClientAgentOptions.AIContextProviders = AIContextProviders as IList<AIContextProvider> ?? new List<AIContextProvider>(AIContextProviders);
        }

        _chatClientAgent = ChatClient.AsAIAgent(chatClientAgentOptions);
        return _chatClientAgent;
    }

    /// <inheritdoc />
    public async Task<AgentSession> GetAgentSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_agentSession is not null)
        {
            return _agentSession;
        }

        if (Session.AgentSession is { } existingSession)
        {
            _agentSession = existingSession;
            return _agentSession;
        }

        ChatClientAgent chatClientAgent = await GetChatClientAgentAsync(cancellationToken).ConfigureAwait(false);
        _agentSession = await chatClientAgent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        Session.SetAgentSession(_agentSession);
        return _agentSession;
    }

    /// <inheritdoc />
    public void AppendResponseUpdate(AgentResponseUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (_isFirstUpdate)
        {
            _isFirstUpdate = false;
            if (AssistantChatMessage.Content == CopilotChatMessage.PlaceholderContent)
            {
                AssistantChatMessage.ClearMessageItems();
            }
        }

        CopilotChatManager.AppendAssistantResponseUpdate(AssistantChatMessage, update);
    }

    /// <inheritdoc />
    public async Task AppendMessagesToSessionAsync()
    {
        await ChatManager.AppendMessageAsync(Session, UserChatMessage).ConfigureAwait(false);
        await ChatManager.AppendMessageAsync(Session, AssistantChatMessage).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IDisposable StartChatting()
    {
        return ChatManager.StartChatting();
    }


}
