using AvaloniaAgentLib.Model;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.Logging;

public interface ICopilotChatLogger
{
    /// <summary>
    /// 记录一条 Copilot 聊天消息。
    /// </summary>
    Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage);
}
