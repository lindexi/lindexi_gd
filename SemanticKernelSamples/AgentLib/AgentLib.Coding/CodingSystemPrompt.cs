using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal static partial class CodingSystemPrompt
{
    internal static void EnsureInitialized(AgentSession agentSession)
    {
        ArgumentNullException.ThrowIfNull(agentSession);
        if (agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages)
            && messages.Any(message =>
                message.Role == ChatRole.System
                && message.Text.Contains("When asked for your name, you must respond with \"GitHub Copilot\".")))
        {
            return;
        }

        var initializedMessages = new List<ChatMessage>((messages?.Count ?? 0) + 3)
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.System, CodePrompt),
            new(ChatRole.System, SandboxPrompt),
        };
        if (messages is not null)
        {
            initializedMessages.AddRange(messages);
        }

        agentSession.SetInMemoryChatHistory(initializedMessages);
    }
}
