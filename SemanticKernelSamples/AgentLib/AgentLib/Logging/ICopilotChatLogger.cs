using AgentLib.Model;

using System;
using System.Threading.Tasks;

namespace AgentLib.Logging;

/// <summary>
/// 提供 Copilot 聊天消息的日志记录能力。
/// </summary>
public interface ICopilotChatLogger
{
    /// <summary>
    /// 记录一条 Copilot 聊天消息。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="chatMessage">聊天消息。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage);
}
