using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AgentLib.ChatRoom;

/// <summary>
/// 聊天室共享会话。维护所有角色的公开消息列表，支持增量消息提取。
/// </summary>
public sealed class ChatRoomSession : NotifyBase
{
    private string _title = "聊天室";

    /// <summary>
    /// 使用指定的会话 ID 和创建时间创建聊天室会话。
    /// </summary>
    /// <param name="sessionId">会话唯一标识。</param>
    /// <param name="createdAt">创建时间。</param>
    public ChatRoomSession(string sessionId, DateTimeOffset createdAt)
    {
        SessionId = sessionId;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// 使用新生成的会话 ID 和当前时间创建聊天室会话。
    /// </summary>
    public ChatRoomSession()
        : this(Guid.NewGuid().ToString("N"), DateTimeOffset.Now)
    {
    }

    /// <summary>
    /// 会话唯一标识。
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// 聊天室标题。
    /// </summary>
    public string Title
    {
        get => _title;
        set
        {
            if (SetField(ref _title, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>
    /// 用于显示的文本，包含标题和创建时间。
    /// </summary>
    public string DisplayText => $"{Title} {CreatedAt:MM-dd HH:mm}";

    /// <summary>
    /// 公开消息列表。
    /// </summary>
    public ObservableCollection<ChatRoomMessage> Messages { get; } = [];

    /// <summary>
    /// 记录每个角色上次发言的时间戳。用于增量消息提取。
    /// </summary>
    private readonly Dictionary<string, DateTimeOffset> _lastSpeakTimeByRole = [];

    /// <summary>
    /// 向会话中添加一条公开消息，并更新角色的上次发言时间。
    /// </summary>
    /// <param name="message">要添加的消息。</param>
    public void AddMessage(ChatRoomMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Messages.Add(message);

        if (!string.IsNullOrEmpty(message.SenderRoleId))
        {
            _lastSpeakTimeByRole[message.SenderRoleId] = message.Timestamp;
        }
    }

    /// <summary>
    /// 获取自指定角色上次发言之后的所有公开消息（不包含该角色自己的历史发言）。
    /// 用于构建增量消息注入给该角色的 <see cref="CopilotChatManager"/>。
    /// </summary>
    /// <param name="roleId">角色 Id。</param>
    /// <returns>自该角色上次发言之后的所有公开消息。如果该角色从未发言过，返回所有消息。</returns>
    public IReadOnlyList<ChatRoomMessage> GetMessagesSinceLastSpeak(string roleId)
    {
        ArgumentNullException.ThrowIfNull(roleId);

        if (!_lastSpeakTimeByRole.TryGetValue(roleId, out DateTimeOffset lastSpeakTime))
        {
            // 该角色从未发言过，返回所有公开消息
            return Messages.ToList();
        }

        // 返回该角色上次发言时间之后的所有消息
        return Messages
            .Where(m => m.Timestamp > lastSpeakTime)
            .ToList();
    }

    /// <summary>
    /// 检查指定角色是否曾在此聊天室中发言过。
    /// </summary>
    /// <param name="roleId">角色 Id。</param>
    public bool HasRoleSpoken(string roleId)
    {
        ArgumentNullException.ThrowIfNull(roleId);
        return _lastSpeakTimeByRole.ContainsKey(roleId);
    }

    /// <summary>
    /// 从持久化数据恢复聊天室会话。
    /// </summary>
    /// <param name="data">持久化数据。</param>
    public static ChatRoomSession FromPersistence(ChatRoomSessionData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var session = new ChatRoomSession(data.SessionId, data.CreatedAt)
        {
            Title = data.Title,
        };

        // 恢复消息并重建 LastSpeakTime
        foreach (ChatRoomMessage message in data.Messages)
        {
            session.AddMessage(message);
        }

        return session;
    }

    /// <summary>
    /// 将当前会话导出为持久化数据。
    /// </summary>
    public ChatRoomSessionData ToPersistence(IReadOnlyList<ChatRoomRoleDefinition> roleDefinitions)
    {
        return new ChatRoomSessionData
        {
            SessionId = SessionId,
            Title = Title,
            CreatedAt = CreatedAt,
            LastActivityAt = Messages.Count > 0
                ? Messages[^1].Timestamp
                : CreatedAt,
            Roles = roleDefinitions.ToList(),
            Messages = Messages.ToList(),
        };
    }
}