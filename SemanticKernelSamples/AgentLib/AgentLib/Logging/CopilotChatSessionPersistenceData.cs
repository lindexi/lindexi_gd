using AgentLib.Model;

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AgentLib.Logging;

/// <summary>
/// 表示从持久化存储读取的 Copilot 会话数据。
/// </summary>
public sealed record CopilotChatSessionPersistenceData
{
    /// <summary>
    /// 获取持久化格式版本。
    /// </summary>
    public required int FormatVersion { get; init; }

    /// <summary>
    /// 获取会话唯一标识符。
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 获取会话开始时间。
    /// </summary>
    public required DateTimeOffset StartedTime { get; init; }

    /// <summary>
    /// 获取持久化的会话标题。
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 获取持久化的公开聊天消息。
    /// </summary>
    public required IReadOnlyList<CopilotChatMessage> Messages { get; init; }

    /// <summary>
    /// 获取序列化的代理会话状态；尚未创建代理状态时为 <see langword="null"/>。
    /// </summary>
    public JsonElement? AgentSessionState { get; init; }
}
