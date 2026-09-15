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

    /// <summary>
    /// 记录一条与指定会话关联的诊断信息。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="category">诊断信息类别。</param>
    /// <param name="message">诊断信息内容。</param>
    /// <param name="exception">相关异常。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task LogDiagnosticAsync(Guid sessionId, string category, string message, Exception? exception = null);
}
