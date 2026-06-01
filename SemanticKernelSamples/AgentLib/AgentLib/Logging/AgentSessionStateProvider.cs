using Microsoft.Agents.AI;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.Logging;

internal sealed class AgentSessionStateProvider : ICopilotChatSessionStateProvider
{
    private readonly ChatClientAgent _chatClientAgent;
    private readonly AgentSession _agentSession;

    public AgentSessionStateProvider(ChatClientAgent chatClientAgent, AgentSession agentSession)
    {
        _chatClientAgent = chatClientAgent ?? throw new ArgumentNullException(nameof(chatClientAgent));
        _agentSession = agentSession ?? throw new ArgumentNullException(nameof(agentSession));
    }

    public async Task<JsonElement?> GetSerializedSessionStateAsync(CancellationToken cancellationToken = default)
    {
        JsonElement serializedSessionState = await _chatClientAgent
            .SerializeSessionAsync(_agentSession, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return serializedSessionState;
    }
}