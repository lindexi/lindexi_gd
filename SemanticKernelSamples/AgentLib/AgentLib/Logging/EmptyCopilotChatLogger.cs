using AgentLib.Model;

namespace AgentLib.Logging;

class EmptyCopilotChatLogger : ICopilotChatLogger
{
    public Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage)
    {
        return Task.CompletedTask;
    }

    public Task LogDiagnosticAsync(Guid sessionId, string category, string message, Exception? exception = null)
    {
        return Task.CompletedTask;
    }
}