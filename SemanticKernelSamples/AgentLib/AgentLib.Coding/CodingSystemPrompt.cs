using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal static class CodingSystemPrompt
{
    internal static async Task EnsureSystemPromptInSessionAsync
    (
        AgentSession agentSession,
        string? copilotInstructionsPath,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(agentSession);
        if (agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages)
            && messages.Any
            (message =>
                message.Role == ChatRole.System
                && message.Text.Contains("When asked for your name, you must respond with \"GitHub Copilot\".")
            ))
        {
            return;
        }

        IReadOnlyList<string> prompts = await CodingPromptProvider
            .BuildAsync(copilotInstructionsPath, cancellationToken).ConfigureAwait(false);
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
    }
}
