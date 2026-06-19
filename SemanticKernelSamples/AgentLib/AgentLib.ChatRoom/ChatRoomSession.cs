using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 聊天室共享会话。维护所有角色的公开消息列表，支持增量消息提取。
/// </summary>
public sealed class ChatRoomSession : NotifyBase
{
    private string _title = "聊天室";
    private IMainThreadDispatcher? _mainThreadDispatcher;
    private ChatRoomMessage? _streamingMessage;

    /// <summary>
    /// 使用指定的会话 ID 和创建时间创建聊天室会话。
    /// </summary>
    /// <param name="sessionId">会话唯一标识。</param>
    /// <param name="createdAt">创建时间。</param>
    public ChatRoomSession(Guid sessionId, DateTimeOffset createdAt)
    {
        SessionId = sessionId;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// 使用新生成的会话 ID 和当前时间创建聊天室会话。
    /// </summary>
    public ChatRoomSession()
        : this(Guid.NewGuid(), DateTimeOffset.Now)
    {
    }

    /// <summary>
    /// 会话唯一标识。
    /// </summary>
    public Guid SessionId { get; }

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
    /// 当前正在流式生成的消息。角色发言期间设置此属性，发言完成后置为 <see langword="null"/>。
    /// UI 绑定此属性的 <see cref="ChatRoomMessage.CopilotChatMessage"/> 即可感知流式内容更新。
    /// </summary>
    public ChatRoomMessage? StreamingMessage
    {
        get => _streamingMessage;
        set => SetField(ref _streamingMessage, value);
    }

    /// <summary>
    /// 主线程调度器。设置后，<see cref="AddMessage"/> 将通过调度器回到主线程修改 <see cref="Messages"/>。
    /// 为 <see langword="null"/> 时直接在当前线程执行。仅在构造期可设置。
    /// </summary>
    public IMainThreadDispatcher? MainThreadDispatcher
    {
        get => _mainThreadDispatcher;
        init => _mainThreadDispatcher = value;
    }

    /// <summary>
    /// 记录每个角色上次发言的时间戳。用于增量消息提取。
    /// </summary>
    private readonly Dictionary<string, DateTimeOffset> _lastSpeakTimeByRole = [];

    /// <summary>
    /// 向会话中添加一条公开消息，并更新角色的上次发言时间。
    /// 如果设置了 <see cref="MainThreadDispatcher"/>，将调度到主线程执行。
    /// </summary>
    /// <param name="message">要添加的消息。</param>
    public async Task AddMessageAsync(ChatRoomMessage message)
    {
        if (_mainThreadDispatcher is not null)
        {
            await _mainThreadDispatcher.InvokeAsync(() =>
            {
                AddMessageCore(message);
                return Task.CompletedTask;
            });
            return;
        }

        AddMessageCore(message);
    }

    /// <summary>
    /// 同步添加消息，仅在确定无 dispatcher 的构造期使用。
    /// </summary>
    internal void AddMessage(ChatRoomMessage message)
    {
        AddMessageCore(message);
    }

    private void AddMessageCore(ChatRoomMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

#if DEBUG
        if (_mainThreadDispatcher is not null && !_mainThreadDispatcher.CheckAccess())
        {
            Debug.Fail("ChatRoomSession.AddMessageAsync 不在主线程上执行，但已设置 MainThreadDispatcher。");
        }
#endif

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