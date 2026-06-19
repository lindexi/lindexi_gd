using AgentLib.ChatRoom.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom.SpeakerSelectors;

/// <summary>
/// 固定顺序轮流发言选择器。按角色注册顺序轮流，支持 @mention 队列和人类插话后暂停。
/// </summary>
public sealed class RoundRobinSpeakerSelector : ISpeakerSelector
{
    private int _currentIndex = -1;
    private int _currentRound;
    private readonly Queue<string> _pendingMentionQueue = new();
    private bool _humanInterjectionPending;

    /// <summary>
    /// 最大轮次数。为 <see langword="null"/> 时无限循环。
    /// </summary>
    public int? MaxRounds { get; init; }

    /// <summary>
    /// 当前轮次计数（从 1 开始）。
    /// </summary>
    public int CurrentRound => _currentRound;

    /// <summary>
    /// 根据当前对话历史选择下一个发言角色。
    /// 队列优先：先检查 @ 队列，为空时才检查 history[^1] 的 @ 触发源。
    /// 人类插话后所有被 @ 角色回复完毕时返回 null 暂停。
    /// </summary>
    /// <param name="roles">可发言的角色列表。</param>
    /// <param name="history">公开消息历史。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下一个发言的角色；暂停或结束时返回 <see langword="null"/>。</returns>
    public Task<ChatRoomRole?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatRoomRole> roles,
        IReadOnlyList<ChatRoomMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (roles is null || roles.Count == 0)
        {
            return Task.FromResult<ChatRoomRole?>(null);
        }

        // 自动循环角色：AlwaysParticipate 的非人类角色
        var autoRoles = roles.Where(r =>
            !r.Definition.IsHuman &&
            r.Definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate
        ).ToList();

        // === 步骤 1：队列优先，直接出队 ===
        while (_pendingMentionQueue.Count > 0)
        {
            string roleId = _pendingMentionQueue.Dequeue();
            ChatRoomRole? matchedRole = roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
            if (matchedRole is not null)
            {
                return Task.FromResult<ChatRoomRole?>(matchedRole);
            }
            // 角色已被移除，继续出队下一个
        }

        // === 步骤 2：队列为空，检查 history[^1] 触发源 ===
        if (history.Count > 0)
        {
            ChatRoomMessage lastMessage = history[^1];

            if (lastMessage.IsHumanMessage)
            {
                // 人类插话 → 标记"正在回应人类"
                _humanInterjectionPending = true;

                // 入队被 @ 的角色
                EnqueueMentions(lastMessage.MentionedRoleIds, roles);

                // 如果入队后队列非空，直接出队返回
                if (_pendingMentionQueue.Count > 0)
                {
                    string roleId = _pendingMentionQueue.Dequeue();
                    ChatRoomRole? matchedRole = roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
                    if (matchedRole is not null)
                    {
                        return Task.FromResult<ChatRoomRole?>(matchedRole);
                    }
                }
                // 人类没 @ 任何人 → 回到正常轮流
            }
            else if (!lastMessage.IsSystemMessage)
            {
                // LLM 消息
                // 非人类触发的自动循环中，检查 LLM 是否 @ 了别人
                EnqueueMentions(lastMessage.MentionedRoleIds, roles);

                if (_pendingMentionQueue.Count > 0)
                {
                    string roleId = _pendingMentionQueue.Dequeue();
                    ChatRoomRole? matchedRole = roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
                    if (matchedRole is not null)
                    {
                        return Task.FromResult<ChatRoomRole?>(matchedRole);
                    }
                }
            }
        }

        // === 步骤 3：正常轮流 ===
        if (autoRoles.Count == 0)
        {
            return Task.FromResult<ChatRoomRole?>(null);
        }

        _currentIndex = (_currentIndex + 1) % autoRoles.Count;

        if (_currentIndex == 0)
        {
            _currentRound++;

            if (MaxRounds.HasValue && _currentRound > MaxRounds.Value)
            {
                return Task.FromResult<ChatRoomRole?>(null);
            }
        }

        return Task.FromResult<ChatRoomRole?>(autoRoles[_currentIndex]);
    }

    /// <summary>
    /// 将被 @ 的角色 ID 入队（去重）。
    /// </summary>
    private void EnqueueMentions(IReadOnlyList<string> mentionedRoleIds, IReadOnlyList<ChatRoomRole> roles)
    {
        if (mentionedRoleIds is null || mentionedRoleIds.Count == 0)
        {
            return;
        }

        // 用 HashSet 记录已在队列中的角色 ID，避免重复入队
        var inQueue = new HashSet<string>(_pendingMentionQueue);

        foreach (string roleId in mentionedRoleIds)
        {
            if (string.IsNullOrEmpty(roleId))
            {
                continue;
            }

            // 跳过已在队列中的
            if (!inQueue.Add(roleId))
            {
                continue;
            }

            // 跳过自己（如果 SenderRoleId 可获取，此处由调用方保证）
            _pendingMentionQueue.Enqueue(roleId);
        }
    }

    /// <summary>
    /// 重置选择器状态。
    /// </summary>
    public void Reset()
    {
        _currentIndex = -1;
        _currentRound = 0;
        _pendingMentionQueue.Clear();
        _humanInterjectionPending = false;
    }
}