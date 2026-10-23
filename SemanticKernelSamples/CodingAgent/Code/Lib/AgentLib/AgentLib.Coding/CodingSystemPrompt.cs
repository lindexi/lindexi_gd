using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal static class CodingSystemPrompt
{
    internal static Task EnsureSystemPromptInSessionAsync
    (
        AgentSession agentSession,
        IReadOnlyList<string> prompts,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(agentSession);
        if (agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages)
            && messages.Any(message => message.Role == ChatRole.System))
        {
            return Task.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var initializedMessages = new List<ChatMessage>((messages?.Count ?? 0) + prompts.Count);
        foreach (string prompt in prompts)
        {
            initializedMessages.Add(new ChatMessage(ChatRole.System, prompt));
        }
        if (messages is not null)
        {
            initializedMessages.AddRange(messages);
        }

        agentSession.SetInMemoryChatHistory(initializedMessages);
        return Task.CompletedTask;
    }
}
