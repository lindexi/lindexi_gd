using System;

namespace AgentLib.Logging;

/// <summary>
/// 表示持久化 Copilot 会话的列表摘要。
/// </summary>
public sealed record CopilotChatSessionSummary
{
    /// <summary>
    /// 获取会话唯一标识符。
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 获取会话标题。
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 获取会话开始时间。
    /// </summary>
    public required DateTimeOffset StartedTime { get; init; }

    /// <summary>
    /// 获取会话消息数。
    /// </summary>
    public required int MessageCount { get; init; }
}
