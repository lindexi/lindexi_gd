using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.ChatRoom.Model;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 会话摘要信息。用于会话列表展示。
/// </summary>
public sealed class SessionSummary
{
    /// <summary>
    /// 会话唯一标识。
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
    /// 角色数量。
    /// </summary>
    public int RoleCount { get; init; }

    /// <summary>
    /// 消息数量。
    /// </summary>
    public int MessageCount { get; init; }
}

/// <summary>
/// 会话列表管理服务。封装 <see cref="ChatRoomPersistence"/> 的会话列表操作。
/// </summary>
public sealed class SessionService
{
    private readonly ChatRoomPersistence _persistence;

    /// <summary>
    /// 使用指定的持久化管理器创建会话服务。
    /// </summary>
    /// <param name="persistence">聊天室持久化管理器。</param>
    public SessionService(ChatRoomPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(persistence);
        _persistence = persistence;
    }

    /// <summary>
    /// 列出所有历史会话摘要。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按创建时间降序排列的历史会话摘要。</returns>
    public async Task<IReadOnlyList<SessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> sessionIds = _persistence.ListSessionIds();
        var summaries = new List<SessionSummary>(sessionIds.Count);

        foreach (string sessionId in sessionIds)
        {
            ChatRoomSessionData? data = await _persistence
                .LoadConfigAsync(sessionId, cancellationToken)
                .ConfigureAwait(false);
            if (data is null)
            {
                continue;
            }

            // 过滤空会话：不显示没有任何聊天记录的会话
            if (data.Messages.Count == 0)
            {
                continue;
            }

            summaries.Add(new SessionSummary
            {
                SessionId = sessionId,
                Title = data.Title,
                CreatedAt = data.CreatedAt,
                RoleCount = data.Roles.Count,
                MessageCount = data.Messages.Count,
            });
        }

        return summaries
            .OrderByDescending(s => s.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// 删除指定会话的所有持久化数据。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    public void DeleteSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("会话 ID 不能为空。", nameof(sessionId));
        }
        _persistence.Delete(sessionId);
    }

    /// <summary>
    /// 加载指定会话的持久化数据。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话持久化数据；不存在时返回 <see langword="null"/>。</returns>
    public async Task<ChatRoomSessionData?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("会话 ID 不能为空。", nameof(sessionId));
        }
        return await _persistence.LoadConfigAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }
}
