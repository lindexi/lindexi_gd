using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AgentLib.ChatRoom.Model;

/// <summary>
/// 聊天室中的公开消息模型。只包含公开可见的文本内容，不包含角色的内部思考和工具调用细节。
/// 继承 <see cref="NotifyBase"/>，支持属性变更通知供 UI 绑定。
/// 当 <see cref="CopilotChatMessage"/> 关联时，自动桥接其 <see cref="CopilotChatMessage.Content"/> 变更。
/// </summary>
public sealed class ChatRoomMessage : NotifyBase
{
    private string _staticContent = string.Empty;
    private IReadOnlyList<string> _mentionedRoleIds = Array.Empty<string>();
    private CopilotChatMessage? _copilotChatMessage;
    private bool _isStreaming;

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
    public string StaticContent
    {
        get => _staticContent;
        set => SetField(ref _staticContent, value);
    }

    /// <summary>
    /// 消息内容（纯文本，公开可见）。有 <see cref="CopilotChatMessage"/> 时委托返回其实时内容，否则返回 <see cref="StaticContent"/>。
    /// 当 <see cref="CopilotChatMessage"/> 的 <see cref="CopilotChatMessage.Content"/> 更新时自动触发属性变更通知。
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
    public IReadOnlyList<string> MentionedRoleIds
    {
        get => _mentionedRoleIds;
        set => SetField(ref _mentionedRoleIds, value);
    }

    /// <summary>
    /// 关联的底层 <see cref="CopilotChatMessage"/>。
    /// 仅 AI 角色消息可能携带此对象，用于 UI 直接绑定流式属性（如 Content、Token 用量等）。
    /// 为 <see langword="null"/> 时表示该消息无底层对象（如人类消息、系统消息）。
    /// 设置时自动订阅/退订其 <see cref="INotifyPropertyChanged.PropertyChanged"/>，桥接 <see cref="Content"/> 变更。
    /// <para>持久化时忽略此属性，消息文本内容通过 <see cref="StaticContent"/> 保存和恢复。
    /// 反序列化后通过 <see cref="RestoreCopilotChatMessage"/> 从 <see cref="StaticContent"/> 重建此对象。</para>
    /// </summary>
    [JsonIgnore]
    public CopilotChatMessage? CopilotChatMessage
    {
        get => _copilotChatMessage;
        set
        {
            if (ReferenceEquals(_copilotChatMessage, value))
            {
                return;
            }

            if (_copilotChatMessage is not null)
            {
                _copilotChatMessage.PropertyChanged -= OnCopilotChatMessagePropertyChanged;
            }

            _copilotChatMessage = value;

            if (_copilotChatMessage is not null)
            {
                _copilotChatMessage.PropertyChanged += OnCopilotChatMessagePropertyChanged;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(Content));
        }
    }

    /// <summary>
    /// 是否正在流式生成中。流式阶段 UI 绑定 <see cref="CopilotChatMessage"/> 的属性感知实时更新；
    /// 完成后由 <see cref="ChatRoomManager"/> 将此属性设为 <see langword="false"/>。
    /// <para>持久化时忽略此属性，属于运行时状态。</para>
    /// </summary>
    [JsonIgnore]
    public bool IsStreaming
    {
        get => _isStreaming;
        set => SetField(ref _isStreaming, value);
    }

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

    /// <summary>
    /// 当关联的 <see cref="CopilotChatMessage"/> 的 <see cref="CopilotChatMessage.Content"/> 变更时，
    /// 桥接通知本消息的 <see cref="Content"/> 属性已变更。
    /// </summary>
    private void OnCopilotChatMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotChatMessage.Content))
        {
            OnPropertyChanged(nameof(Content));
        }
    }

    /// <summary>
    /// 从 <see cref="StaticContent"/> 重建 <see cref="CopilotChatMessage"/>。
    /// 持久化时 <see cref="CopilotChatMessage"/> 被忽略（<see cref="JsonIgnoreAttribute"/>），
    /// 反序列化后为 <see langword="null"/>。对于 AI 消息，需要从 <see cref="StaticContent"/>
    /// 构建一个包含文本片段的 <see cref="CopilotChatMessage"/>，使 UI 绑定的
    /// <see cref="CopilotChatMessage.MessageItems"/> 能正常渲染历史消息内容。
    /// 应在从持久化数据反序列化后由加载链路显式调用。
    /// </summary>
    internal void RestoreCopilotChatMessage()
    {
        if (_copilotChatMessage is null && !string.IsNullOrEmpty(_staticContent) && !IsHumanMessage && !IsSystemMessage)
        {
            CopilotChatMessage = new CopilotChatMessage(ChatRole.Assistant, _staticContent);
        }
    }
}