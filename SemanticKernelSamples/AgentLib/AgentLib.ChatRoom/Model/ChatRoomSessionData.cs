using System;
using System.Collections.Generic;

namespace AgentLib.ChatRoom.Model;

/// <summary>
/// 聊天室会话持久化数据模型（纯 DTO）。用于序列化/反序列化聊天室的完整状态。
/// </summary>
public sealed class ChatRoomSessionData
{
    /// <summary>
    /// 会话唯一标识。
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// 聊天室标题。
    /// </summary>
    public string Title { get; set; } = "聊天室";

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 最后活动时间。
    /// </summary>
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>
    /// 角色定义列表。
    /// </summary>
    public List<ChatRoomRoleDefinition> Roles { get; init; } = [];

    /// <summary>
    /// 公开消息列表（仅文本内容，不含工具调用和思考细节）。
    /// </summary>
    public List<ChatRoomMessage> Messages { get; init; } = [];
}