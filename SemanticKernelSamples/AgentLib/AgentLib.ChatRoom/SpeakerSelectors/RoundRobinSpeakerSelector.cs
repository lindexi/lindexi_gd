using AgentLib.ChatRoom.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom.SpeakerSelectors;

/// <summary>
/// 固定顺序轮流发言选择器。按角色注册顺序轮流，支持 @mention 队列。
/// 人类插话时重置轮流索引，使 AlwaysParticipate 角色从头开始。
/// 通过 <see cref="OnSpeakerResult"/> 接收发言结果反馈，精确管理内部状态。
/// </summary>
public sealed class RoundRobinSpeakerSelector : ISpeakerSelector
{
    private int _currentIndex = -1;
    private int _currentRound;
    private readonly Queue<string> _pendingMentionQueue = new();

    /// <summary>
    /// 当前轮次中已成功发言的角色 ID 集合。
    /// <see cref="OnSpeakerResult"/> 收到 <c>success: true</c> 时加入。
    /// 正常轮流时跳过此集合中的角色；所有可发言角色都在此集合中时返回 <c>null</c>（自然暂停）。
    /// 当 <c>history[^1]</c> 变为人类消息（新一轮开始）时清空。
    /// </summary>
    private readonly HashSet<string> _spokenRoleIdsInCurrentTurn = [];

    /// <summary>
    /// 从 @mention 队列出队但发言失败（<see cref="OnSpeakerResult"/> 收到 <c>success: false</c>）的角色 ID 集合。
    /// 入队和出队时跳过此集合中的角色，防止发言失败后被重复选中导致死循环。
    /// 当 <c>history[^1]</c> 变为人类消息（新一轮开始）时清空，新的 @ 可以重新尝试。
    /// </summary>
    private readonly HashSet<string> _failedMentionRoleIds = [];

    /// <summary>
    /// 上次调用 <see cref="SelectNextSpeakerAsync"/> 时 history 最后一条消息的 MessageId。
    /// 用于检测 <c>history[^1]</c> 是否变化（有新消息追加）。
    /// </summary>
    private string? _lastSeenHistoryLastMessageId;

    /// <summary>
    /// 管理者是否刚发言过且中间没有其他角色发言。
    /// 管理者通过管理者机制发言后设为 true，防止管理者连续发言（无其他角色介入时重复触发）。
    /// 任何非管理者角色发言成功后设为 false，使管理者可以在下一轮"所有角色发言完毕"时再次触发。
    /// 当 history[^1] 变为人类消息（新一轮开始）时清空。
    /// </summary>
    private bool _managerJustSpoke;

    /// <summary>
    /// 标记上一次选择是否为管理者触发。
    /// 用于 OnSpeakerResult 中区分"管理者触发"和"@ 触发"的管理者发言。
    /// </summary>
    private bool _isManagerTrigger;

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
    /// 人类插话时重置轮流索引，使 AlwaysParticipate 角色从头开始。
    /// 正常轮流时跳过 <see cref="_spokenRoleIdsInCurrentTurn"/> 中的角色（本轮已发言过）；
    /// 所有可发言角色都已发言时返回 <see langword="null"/>（自然暂停）。
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
        if (roles.Count == 0)
        {
            return Task.FromResult<ChatRoomRole?>(null);
        }

        // 检测 history[^1] 是否变化（有新消息追加）
        if (history.Count > 0)
        {
            string currentLastMessageId = history[^1].MessageId;
            if (currentLastMessageId != _lastSeenHistoryLastMessageId)
            {
                _lastSeenHistoryLastMessageId = currentLastMessageId;

                // 人类消息标志着新一轮开始：清空已发言和失败记录
                if (history[^1].IsHumanMessage)
                {
                    _spokenRoleIdsInCurrentTurn.Clear();
                    _failedMentionRoleIds.Clear();
                    _managerJustSpoke = false;
                }
            }
        }

        // 自动循环角色：AlwaysParticipate 的非人类角色
        var autoRoles = roles.Where(r =>
            !r.Definition.IsHuman &&
            r.Definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate
        ).ToList();

        // === 步骤 1：队列优先，直接出队 ===
        ChatRoomRole? pendingRole = TryDequeueMention(roles);
        if (pendingRole is not null)
        {
            return Task.FromResult<ChatRoomRole?>(pendingRole);
        }

        // === 步骤 2：队列为空，检查 history[^1] 触发源 ===
        if (history.Count > 0)
        {
            ChatRoomMessage lastMessage = history[^1];

            if (lastMessage.IsHumanMessage)
            {
                // 人类插话 → 重置轮流索引，使 AlwaysParticipate 角色从头开始
                _currentIndex = -1;
            }

            // 人类消息和 LLM 消息统一入队被 @ 的角色并尝试出队
            if (!lastMessage.IsSystemMessage)
            {
                EnqueueMentions(lastMessage.MentionedRoleIds, roles);

                ChatRoomRole? mentionedRole = TryDequeueMention(roles);
                if (mentionedRole is not null)
                {
                    return Task.FromResult<ChatRoomRole?>(mentionedRole);
                }
            }
            // 没人被 @ → 回到正常轮流
        }

        // === 步骤 3：正常轮流 ===
        // 跳过本轮已发言的角色；所有可发言角色都已发言 → 检查管理者
        var availableRoles = autoRoles
            .Where(r => !_spokenRoleIdsInCurrentTurn.Contains(r.Definition.RoleId))
            .ToList();

        if (availableRoles.Count == 0)
        {
            // 所有 AlwaysParticipate 角色都已发言（或没有 AlwaysParticipate 角色）
            // === 管理者检查 ===
            // 仅当 autoRoles 为空（无 AlwaysParticipate 角色，如纯 MentionOnly 管理者场景）
            // 或 autoRoles 中存在非 IsManagerRole 角色时才触发管理者兜底。
            // 如果 autoRoles 全是 IsManagerRole 角色（如单助手场景），管理者已通过正常轮流发言，不再兜底。
            if (!_managerJustSpoke && (autoRoles.Count == 0 || autoRoles.Any(r => !r.Definition.IsManagerRole)))
            {
                ChatRoomRole? managerRole = roles.FirstOrDefault(r => r.Definition.IsManagerRole);
                if (managerRole is not null)
                {
                    _isManagerTrigger = true;
                    return Task.FromResult<ChatRoomRole?>(managerRole);
                }
            }

            // 管理者刚发言且中间无其他角色发言，或没有管理者角色 → 自然暂停
            _isManagerTrigger = false;
            return Task.FromResult<ChatRoomRole?>(null);
        }

        _isManagerTrigger = false;

        _currentIndex = (_currentIndex + 1) % autoRoles.Count;

        // 如果当前索引指向已发言角色，向前推进到第一个未发言的
        while (_spokenRoleIdsInCurrentTurn.Contains(autoRoles[_currentIndex].Definition.RoleId))
        {
            _currentIndex = (_currentIndex + 1) % autoRoles.Count;
        }

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
    /// 通知 Selector 上一个选中的角色的发言结果。
    /// <paramref name="success"/> 为 <see langword="true"/> 时将角色加入 <see cref="_spokenRoleIdsInCurrentTurn"/>；
    /// 为 <see langword="false"/> 时将角色加入 <see cref="_failedMentionRoleIds"/>，防止重复选中。
    /// </summary>
    /// <param name="role">上一个选中的角色。</param>
    /// <param name="success">是否成功发言（产生了有效内容）。</param>
    public void OnSpeakerResult(ChatRoomRole role, bool success)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (success)
        {
            _spokenRoleIdsInCurrentTurn.Add(role.Definition.RoleId);

            if (_isManagerTrigger && role.Definition.IsManagerRole)
            {
                // 管理者通过管理者机制发言，标记防止连续触发
                _managerJustSpoke = true;
            }
            else
            {
                // 非管理者角色发言（或管理者被 @ 触发发言），清除标记
                _managerJustSpoke = false;
            }
        }
        else
        {
            _failedMentionRoleIds.Add(role.Definition.RoleId);

            if (_isManagerTrigger && role.Definition.IsManagerRole)
            {
                // 管理者发言失败也标记，防止死循环
                _managerJustSpoke = true;
            }
        }

        _isManagerTrigger = false;
    }

    /// <summary>
    /// 将被 @ 的角色 ID 入队（去重，跳过已发言和已失败的角色）。
    /// </summary>
    /// <param name="mentionedRoleIds">被 @ 的角色 ID 列表。</param>
    /// <param name="roles">当前角色列表（未使用，保留用于未来扩展）。</param>
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

            // 跳过本轮发言失败的（防止死循环）
            if (_failedMentionRoleIds.Contains(roleId))
            {
                continue;
            }

            _pendingMentionQueue.Enqueue(roleId);
        }
    }

    /// <summary>
    /// 从 mention 队列出队一个有效角色。跳过已被移除和已发言失败的角色。
    /// </summary>
    /// <param name="roles">当前角色列表，用于验证角色是否仍存在。</param>
    /// <returns>匹配到的角色；队列空或角色均已移除/失败时返回 <see langword="null"/>。</returns>
    private ChatRoomRole? TryDequeueMention(IReadOnlyList<ChatRoomRole> roles)
    {
        while (_pendingMentionQueue.Count > 0)
        {
            string roleId = _pendingMentionQueue.Dequeue();

            // 跳过发言失败的角色
            if (_failedMentionRoleIds.Contains(roleId))
            {
                continue;
            }

            ChatRoomRole? matchedRole = roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
            if (matchedRole is not null)
            {
                return matchedRole;
            }
            // 角色已被移除，继续出队下一个
        }

        return null;
    }

    /// <summary>
    /// 重置选择器状态。
    /// </summary>
    public void Reset()
    {
        _currentIndex = -1;
        _currentRound = 0;
        _pendingMentionQueue.Clear();
        _spokenRoleIdsInCurrentTurn.Clear();
        _failedMentionRoleIds.Clear();
        _managerJustSpoke = false;
        _isManagerTrigger = false;
        _lastSeenHistoryLastMessageId = null;
    }
}