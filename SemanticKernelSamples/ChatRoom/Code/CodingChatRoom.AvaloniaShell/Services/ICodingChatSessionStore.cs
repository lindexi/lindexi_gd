using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AgentLib;
using AgentLib.Core;
using AgentLib.Logging;
using AgentLib.Model;

using Microsoft.Agents.AI;

using System.Text.Json;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingChatSessionStore
{
    Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default);

    Task<CopilotChatSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task SaveSessionAsync(CopilotChatSession session, CancellationToken cancellationToken = default);
}

internal sealed class FileCodingChatSessionStore : ICodingChatSessionStore
{
    private readonly CopilotChatManager _chatManager;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly FileCopilotChatSessionStore _store;

    public FileCodingChatSessionStore(
        string sessionDirectory,
        string logDirectory,
        CopilotChatManager chatManager,
        IMainThreadDispatcher mainThreadDispatcher)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(mainThreadDispatcher);
        _chatManager = chatManager;
        _mainThreadDispatcher = mainThreadDispatcher;
        _store = new FileCopilotChatSessionStore(sessionDirectory, logDirectory);
    }

    public Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
        => _store.ListSessionsAsync(cancellationToken);

    public async Task<CopilotChatSession> LoadSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        CopilotChatSessionPersistenceData persistenceData = await _store
            .LoadSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);
        var session = new CopilotChatSession(persistenceData.SessionId, persistenceData.StartedTime)
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };
        if (!string.IsNullOrWhiteSpace(persistenceData.Title))
        {
            session.SetTitle(persistenceData.Title);
        }

        foreach (CopilotChatMessage message in persistenceData.Messages)
        {
            await session.AddMessageAsync(message).ConfigureAwait(false);
        }

        if (persistenceData.AgentSessionState is JsonElement agentSessionState)
        {
            IManualSendMessageContext context = await _chatManager
                .CreateManualSendMessageContextAsync(cancellationToken)
                .ConfigureAwait(false);
            ChatClientAgent agent = await context
                .GetChatClientAgentAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            AgentSession agentSession = await agent
                .DeserializeSessionAsync(agentSessionState, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            session.SetAgentSession(agentSession);
        }

        return session;
    }

    public Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => _store.DeleteSessionAsync(sessionId, cancellationToken);

    public async Task SaveSessionAsync(CopilotChatSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        JsonElement? agentSessionState = null;
        if (session.AgentSession is AgentSession agentSession)
        {
            IManualSendMessageContext context = await _chatManager
                .CreateManualSendMessageContextAsync(cancellationToken)
                .ConfigureAwait(false);
            ChatClientAgent agent = await context
                .GetChatClientAgentAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            JsonElement state = await agent
                .SerializeSessionAsync(agentSession, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            agentSessionState = state.Clone();
        }

        await _store.SaveSessionAsync(session, agentSessionState, cancellationToken).ConfigureAwait(false);
    }
}