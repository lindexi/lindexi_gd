using AgentLib.Model;

using System;
using System.Threading.Tasks;

namespace AgentLib.Logging;

public interface ICopilotChatLogger
{
    /// <summary>
    /// 记录一条 Copilot 聊天消息。
    /// </summary>
    Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage,
        ICopilotChatSessionStateProvider? agentSessionStateProvider = null);
}
