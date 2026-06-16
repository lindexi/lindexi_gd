using AgentLib;
using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using ChatRoomAvaloniaDemo.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatRoomAvaloniaDemo.Services;

/// <summary>
/// 聊天室核心服务层。封装 <see cref="ChatRoomManager"/> 和 <see cref="ChatRoomPersistence"/> 的生命周期，
/// 为 ViewModel 层提供简化的会话管理、启停控制和人类插话接口。
/// </summary>
public sealed class ChatRoomService
{
    private ChatRoomManager? _chatRoomManager;
    private ChatRoomPersistence? _persistence;
    private AppConfig? _appConfig;

    /// <summary>
    /// 当前聊天室管理器。为 <see langword="null"/> 时表示尚未创建或加载会话。
    /// </summary>
    public ChatRoomManager? ChatRoomManager => _chatRoomManager;

    /// <summary>
    /// 持久化管理器。为 <see langword="null"/> 时表示未配置持久化路径。
    /// </summary>
    public ChatRoomPersistence? Persistence => _persistence;

    /// <summary>
    /// 当前应用配置。
    /// </summary>
    public AppConfig? AppConfig => _appConfig;

    /// <summary>
    /// 是否有活跃的会话。
    /// </summary>
    public bool HasActiveSession => _chatRoomManager is not null;

    /// <summary>
    /// 应用配置，并初始化持久化管理器。
    /// </summary>
    /// <param name="appConfig">应用配置。</param>
    public void ApplyConfig(AppConfig appConfig)
    {
        ArgumentNullException.ThrowIfNull(appConfig);
        _appConfig = appConfig;

        if (!string.IsNullOrWhiteSpace(appConfig.PersistenceBasePath))
        {
            _persistence = new ChatRoomPersistence(appConfig.PersistenceBasePath);
        }
    }

    /// <summary>
    /// 创建新的聊天室会话。
    /// </summary>
    /// <param name="mainThreadDispatcher">主线程调度器，用于 UI 线程安全。</param>
    /// <returns>新创建的 <see cref="ChatRoomManager"/>。</returns>
    public ChatRoomManager CreateNewSession(IMainThreadDispatcher? mainThreadDispatcher = null)
    {
        var session = new ChatRoomSession
        {
            MainThreadDispatcher = mainThreadDispatcher,
        };

        _chatRoomManager = new ChatRoomManager(session)
        {
            Persistence = _persistence,
        };

        if (_appConfig is not null)
        {
            _chatRoomManager.SpeakerSelector = new AgentLib.ChatRoom.SpeakerSelectors.RoundRobinSpeakerSelector
            {
                MaxRounds = _appConfig.DefaultMaxRounds,
            };

            // 创建默认"助手"角色
            var defaultRoleDef = new ChatRoomRoleDefinition
            {
                RoleId = Guid.NewGuid().ToString(),
                RoleName = "助手",
                SystemPrompt = "你是一个智能助手，参与多角色讨论。",
                ModelProviderId = _appConfig.DefaultModelProviderName,
                ModelId = _appConfig.PrimaryModelId,
            };

            var defaultRole = new ChatRoomRole(defaultRoleDef)
            {
                MainThreadDispatcher = mainThreadDispatcher,
            };

            _chatRoomManager.Roles.Add(defaultRole);
        }

        return _chatRoomManager;
    }

    /// <summary>
    /// 从持久化加载指定会话。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="mainThreadDispatcher">主线程调度器。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>加载的 <see cref="ChatRoomManager"/>；如果会话不存在则返回 <see langword="null"/>。</returns>
    public async Task<ChatRoomManager?> LoadSessionAsync(
        string sessionId,
        IMainThreadDispatcher? mainThreadDispatcher = null,
        CancellationToken cancellationToken = default)
    {
        if (_persistence is null)
        {
            return null;
        }

        ChatRoomSessionData? data = await _persistence.LoadConfigAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (data is null)
        {
            return null;
        }

        var session = new ChatRoomSession(data.SessionId, data.CreatedAt)
        {
            Title = data.Title,
            MainThreadDispatcher = mainThreadDispatcher,
        };

        foreach (ChatRoomMessage message in data.Messages)
        {
            await session.AddMessageAsync(message).ConfigureAwait(false);
        }

        _chatRoomManager = new ChatRoomManager(session)
        {
            Persistence = _persistence,
        };

        // 恢复角色
        foreach (ChatRoomRoleDefinition roleDef in data.Roles)
        {
            var role = new ChatRoomRole(roleDef)
            {
                MainThreadDispatcher = mainThreadDispatcher,
            };
            await role.InitializeAsync(cancellationToken).ConfigureAwait(false);
            _chatRoomManager.Roles.Add(role);
        }

        if (_appConfig is not null)
        {
            _chatRoomManager.SpeakerSelector = new AgentLib.ChatRoom.SpeakerSelectors.RoundRobinSpeakerSelector
            {
                MaxRounds = _appConfig.DefaultMaxRounds,
            };
        }

        return _chatRoomManager;
    }

    /// <summary>
    /// 获取所有已持久化的会话 ID 列表。
    /// </summary>
    public IReadOnlyList<string> ListSessionIds()
    {
        return _persistence?.ListSessionIds() ?? Array.Empty<string>();
    }

    /// <summary>
    /// 获取历史会话摘要列表（仅含标题和时间，不加载完整数据）。
    /// </summary>
    public async Task<IReadOnlyList<SessionSummary>> ListSessionSummariesAsync(CancellationToken cancellationToken = default)
    {
        if (_persistence is null)
        {
            return Array.Empty<SessionSummary>();
        }

        var sessionIds = _persistence.ListSessionIds();
        var summaries = new List<SessionSummary>(sessionIds.Count);

        foreach (string sessionId in sessionIds)
        {
            ChatRoomSessionData? data = await _persistence.LoadConfigAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (data is not null)
            {
                summaries.Add(new SessionSummary
                {
                    SessionId = sessionId,
                    Title = data.Title,
                    CreatedAt = data.CreatedAt,
                    LastActivityAt = data.LastActivityAt,
                    RoleCount = data.Roles.Count,
                    MessageCount = data.Messages.Count,
                });
            }
        }

        return summaries.OrderByDescending(s => s.LastActivityAt).ToList();
    }

    /// <summary>
    /// 删除指定会话。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    public void DeleteSession(string sessionId)
    {
        _persistence?.Delete(sessionId);
    }

    /// <summary>
    /// 启动自动循环。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task StartAutoLoopAsync(CancellationToken cancellationToken = default)
    {
        if (_chatRoomManager is null)
        {
            return Task.CompletedTask;
        }

        return _chatRoomManager.StartAutoLoopAsync(cancellationToken);
    }

    /// <summary>
    /// 停止自动循环。
    /// </summary>
    public void Stop()
    {
        _chatRoomManager?.Stop();
    }

    /// <summary>
    /// 人类插话。
    /// </summary>
    /// <param name="content">人类输入的内容。</param>
    /// <param name="humanRoleId">人类角色 ID。</param>
    /// <param name="humanRoleName">人类角色显示名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task HumanInterjectAsync(
        string content,
        string humanRoleId = "human",
        string humanRoleName = "我",
        CancellationToken cancellationToken = default)
    {
        if (_chatRoomManager is null)
        {
            return Task.CompletedTask;
        }

        return _chatRoomManager.HumanInterjectAsync(content, humanRoleId, humanRoleName, cancellationToken);
    }

    /// <summary>
    /// 保存当前会话。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return _chatRoomManager?.SaveAsync(cancellationToken) ?? Task.CompletedTask;
    }

    /// <summary>
    /// 关闭当前会话并释放资源。
    /// </summary>
    public void CloseSession()
    {
        _chatRoomManager?.Stop();
        _chatRoomManager = null;
    }
}

/// <summary>
/// 历史会话摘要信息。用于会话列表展示，不包含完整消息数据。
/// </summary>
public sealed class SessionSummary
{
    /// <summary>
    /// 会话 ID。
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// 会话标题。
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 最后活动时间。
    /// </summary>
    public DateTimeOffset LastActivityAt { get; init; }

    /// <summary>
    /// 角色数量。
    /// </summary>
    public int RoleCount { get; init; }

    /// <summary>
    /// 消息数量。
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// 用于列表显示的文本。
    /// </summary>
    public string DisplayText => $"{Title}  {CreatedAt:MM-dd HH:mm}";
}
