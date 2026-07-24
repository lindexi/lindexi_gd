using AgentLib.Model;

namespace AgentLib.Logging;

class EmptyCopilotChatLogger : ICopilotChatLogger
{
    public Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage)
    {
        return Task.CompletedTask;
    }
}