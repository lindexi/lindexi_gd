using AgentLib.Model;

namespace AgentLib.Logging;

class EmptyCopilotChatLogger : ICopilotChatLogger
{
    public Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage, string? serializedAgentSessionState = null)
    {
        return Task.CompletedTask;
    }
}