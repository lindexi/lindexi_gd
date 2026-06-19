using AgentLib.Model;

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
    /// 静态消息内容。当没有关联的 <see cref="CopilotChatMessage"/> 时作为 <see cref="Content"/> 返回。
    /// 持久化时序列化此字段，反序列化时恢复。
    /// </summary>
    public string StaticContent { get; init; } = string.Empty;

    /// <summary>
    /// 消息内容（纯文本，公开可见）。有 <see cref="CopilotChatMessage"/> 时委托返回其实时内容，否则返回 <see cref="StaticContent"/>。
    /// </summary>
    public string Content => CopilotChatMessage?.Content ?? StaticContent;

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
    /// 关联的底层 <see cref="CopilotChatMessage"/>。
    /// 仅 AI 角色消息可能携带此对象，用于 UI 直接绑定流式属性（如 Content、Token 用量等）。
    /// 为 <see langword="null"/> 时表示该消息无底层对象（如人类消息、系统消息）。
    /// </summary>
    public CopilotChatMessage? CopilotChatMessage { get; init; }

    /// <summary>
    /// 是否正在流式生成中。流式阶段 UI 绑定 <see cref="CopilotChatMessage"/> 的属性感知实时更新；
    /// 完成后由 <see cref="ChatRoomSession"/> 将此属性设为 <see langword="false"/>。
    /// </summary>
    public bool IsStreaming { get; set; }

    /// <summary>
    /// 创建系统消息。
    /// </summary>
    /// <param name="content">消息内容。</param>
    public static ChatRoomMessage CreateSystem(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ChatRoomMessage
        {
            StaticContent = content,
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
            StaticContent = content,
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
    /// <param name="copilotChatMessage">关联的底层消息对象，用于 UI 流式绑定。可选。</param>
    public static ChatRoomMessage CreateAssistant(
        string content,
        string roleId,
        string roleName,
        CopilotChatMessage? copilotChatMessage = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ChatRoomMessage
        {
            StaticContent = content,
            SenderRoleId = roleId,
            SenderRoleName = roleName,
            CopilotChatMessage = copilotChatMessage,
        };
    }
}