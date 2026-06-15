using AgentLib.ChatRoom.Model;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 发言选择策略接口。根据当前对话历史决定下一个发言的角色。
/// </summary>
public interface ISpeakerSelector
{
    /// <summary>
    /// 根据当前对话历史决定下一个发言的角色。
    /// 返回 <see langword="null"/> 表示对话自然结束。
    /// </summary>
    /// <param name="roles">可发言的角色列表。</param>
    /// <param name="history">公开消息历史。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下一个发言的角色；<see langword="null"/> 表示对话结束。</returns>
    Task<ChatRoomRole?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatRoomRole> roles,
        IReadOnlyList<ChatRoomMessage> history,
        CancellationToken cancellationToken = default);
}