using AgentLib.ChatRoom.Model;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom.SpeakerSelectors;

/// <summary>
/// 固定顺序轮流发言选择器。按角色注册顺序轮流，支持最大轮次和人类插话后恢复。
/// </summary>
public sealed class RoundRobinSpeakerSelector : ISpeakerSelector
{
    private int _currentIndex = -1;
    private int _currentRound;

    /// <summary>
    /// 最大轮次数。为 <see langword="null"/> 时无限循环。
    /// </summary>
    public int? MaxRounds { get; init; }

    /// <summary>
    /// 当前轮次计数（从 1 开始）。
    /// </summary>
    public int CurrentRound => _currentRound;

    /// <summary>
    /// 当前轮次计数（从 1 开始）。
    /// </summary>
    public Task<ChatRoomRole?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatRoomRole> roles,
        IReadOnlyList<ChatRoomMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (roles is null || roles.Count == 0)
        {
            return Task.FromResult<ChatRoomRole?>(null);
        }

        // 过滤掉人类角色（人类不通过自动循环发言）
        var llmRoles = roles.Where(r => !r.Definition.IsHuman).ToList();
        if (llmRoles.Count == 0)
        {
            return Task.FromResult<ChatRoomRole?>(null);
        }

        // 检查最近一条消息是否是人类插入的
        // 如果是，下一个发言者为上一次发言的 LLM 角色或者从头开始
        if (history.Count > 0 && history[^1].IsHumanMessage)
        {
            // 人类插话后，找到插话前最后发言的 LLM 角色
            int lastLlmIndex = -1;
            for (int i = history.Count - 2; i >= 0; i--)
            {
                if (!history[i].IsHumanMessage && !history[i].IsSystemMessage)
                {
                    string? lastRoleId = history[i].SenderRoleId;
                    if (!string.IsNullOrEmpty(lastRoleId))
                    {
                        lastLlmIndex = llmRoles.FindIndex(r => r.Definition.RoleId == lastRoleId);
                    }
                    break;
                }
            }

            if (lastLlmIndex >= 0)
            {
                // 从该角色之后的下一个角色开始
                _currentIndex = lastLlmIndex;
            }
        }

        // 移到下一个角色
        _currentIndex = (_currentIndex + 1) % llmRoles.Count;

        // 如果 _currentIndex 从 0 开始（第一次进入或绕了一圈），增加轮次
        if (_currentIndex == 0)
        {
            _currentRound++;

            // 检查是否达到最大轮次
            if (MaxRounds.HasValue && _currentRound > MaxRounds.Value)
            {
                return Task.FromResult<ChatRoomRole?>(null);
            }
        }

        return Task.FromResult<ChatRoomRole?>(llmRoles[_currentIndex]);
    }

    /// <summary>
    /// 重置选择器状态。
    /// </summary>
    public void Reset()
    {
        _currentIndex = -1;
        _currentRound = 0;
    }
}