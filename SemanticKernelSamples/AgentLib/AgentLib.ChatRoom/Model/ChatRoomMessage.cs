using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;

namespace AgentLib.ChatRoom.Model;

/// <summary>
/// 聊天室中的公开消息模型。只包含公开可见的文本内容，不包含角色的内部思考和工具调用细节。
/// </summary>
public sealed record ChatRoomMessage
{
    /// <summary>
    /// 消息唯一标识。
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 发言角色 Id。与 <see cref="ChatRoomRoleDefinition.RoleId"/> 对应。
    /// </summary>
    public string SenderRoleId { get; init; } = string.Empty;

    /// <summary>
    /// 发言角色显示名。
    /// </summary>
    public string SenderRoleName { get; init; } = string.Empty;

    /// <summary>
    /// 消息内容（纯文本，公开可见）。
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 消息创建时间戳。
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// 是否人类发送的消息。
    /// </summary>
    public bool IsHumanMessage { get; init; }

    /// <summary>
    /// 是否系统消息（如错误提示、角色发言失败通知等）。
    /// </summary>
    public bool IsSystemMessage { get; init; }

    /// <summary>
    /// 本条消息中 @ 提及的角色 RoleId 列表。
    /// 由 ChatRoomManager 在追加消息时解析填充。
    /// </summary>
    public IReadOnlyList<string> MentionedRoleIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 创建系统消息。
    /// </summary>
    /// <param name="content">消息内容。</param>
    public static ChatRoomMessage CreateSystem(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ChatRoomMessage
        {
            Content = content,
            SenderRoleName = "系统",
            IsSystemMessage = true,
        };
    }

    /// <summary>
    /// 创建人类角色的消息。
    /// </summary>
    /// <param name="content">消息内容。</param>
    /// <param name="humanRoleId">人类角色 Id。</param>
    /// <param name="humanRoleName">人类角色显示名。</param>
    public static ChatRoomMessage CreateHuman(string content, string humanRoleId, string humanRoleName)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ChatRoomMessage
        {
            Content = content,
            SenderRoleId = humanRoleId,
            SenderRoleName = humanRoleName,
            IsHumanMessage = true,
        };
    }

    /// <summary>
    /// 创建 LLM 角色的消息。
    /// </summary>
    /// <param name="content">消息内容（公开文本）。</param>
    /// <param name="roleId">角色 Id。</param>
    /// <param name="roleName">角色显示名。</param>
    public static ChatRoomMessage CreateAssistant(string content, string roleId, string roleName)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ChatRoomMessage
        {
            Content = content,
            SenderRoleId = roleId,
            SenderRoleName = roleName,
        };
    }
}