using AgentLib.ChatRoom.Domain;

namespace AgentLib.ChatRoom.Persistence;

/// <summary>
/// 房间仓储保存结果状态。
/// </summary>
public enum ChatRoomSaveStatus
{
    /// <summary>
    /// 新修订已持久化。
    /// </summary>
    Saved,

    /// <summary>
    /// 相同 CommitId 已经成功持久化。
    /// </summary>
    AlreadyCommitted,

    /// <summary>
    /// 仓储中的修订与调用方期望不一致。
    /// </summary>
    Conflict,

    /// <summary>
    /// 房间已被删除并受墓碑保护，迟到提交不得复活。
    /// </summary>
    Deleted,
}

/// <summary>
/// 房间仓储保存请求。
/// </summary>
public sealed record ChatRoomSaveRequest
{
    /// <summary>
    /// 创建保存请求。
    /// </summary>
    public ChatRoomSaveRequest(
        ChatRoomSnapshot snapshot,
        Guid commitId,
        long expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (commitId == Guid.Empty)
        {
            throw new ArgumentException("提交标识不能为空。", nameof(commitId));
        }
        if (expectedRevision < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        }

        Snapshot = snapshot;
        CommitId = commitId;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>
    /// 待保存的一致快照。
    /// </summary>
    public ChatRoomSnapshot Snapshot { get; }

    /// <summary>
    /// 幂等提交标识。
    /// </summary>
    public Guid CommitId { get; }

    /// <summary>
    /// 调用方期望覆盖的仓储修订；创建新房间时为 -1。
    /// </summary>
    public long ExpectedRevision { get; }
}

/// <summary>
/// 房间仓储保存结果。
/// </summary>
public sealed record ChatRoomSaveResult
{
    /// <summary>
    /// 创建保存结果。
    /// </summary>
    public ChatRoomSaveResult(
        ChatRoomSaveStatus status,
        Guid roomId,
        Guid commitId,
        long durableRevision)
    {
        if (!Enum.IsDefined(typeof(ChatRoomSaveStatus), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }
        if (commitId == Guid.Empty)
        {
            throw new ArgumentException("提交标识不能为空。", nameof(commitId));
        }
        if (durableRevision < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(durableRevision));
        }

        Status = status;
        RoomId = roomId;
        CommitId = commitId;
        DurableRevision = durableRevision;
    }

    /// <summary>
    /// 保存状态。
    /// </summary>
    public ChatRoomSaveStatus Status { get; }

    /// <summary>
    /// 房间标识。
    /// </summary>
    public Guid RoomId { get; }

    /// <summary>
    /// 提交标识。
    /// </summary>
    public Guid CommitId { get; }

    /// <summary>
    /// 当前持久化修订号。
    /// </summary>
    public long DurableRevision { get; }
}

/// <summary>
/// 房间列表摘要。
/// </summary>
public sealed record ChatRoomSummary
{
    /// <summary>
    /// 创建房间摘要。
    /// </summary>
    public ChatRoomSummary(
        Guid roomId,
        string title,
        DateTimeOffset createdAt,
        DateTimeOffset lastActivityAt,
        long revision,
        int roleCount,
        int messageCount)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("房间标题不能为空或空白。", nameof(title));
        }
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }
        if (roleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleCount));
        }
        if (messageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(messageCount));
        }

        RoomId = roomId;
        Title = title.Trim();
        CreatedAt = createdAt;
        LastActivityAt = lastActivityAt;
        Revision = revision;
        RoleCount = roleCount;
        MessageCount = messageCount;
    }

    /// <summary>
    /// 房间标识。
    /// </summary>
    public Guid RoomId { get; }

    /// <summary>
    /// 房间标题。
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// 最后活动时间。
    /// </summary>
    public DateTimeOffset LastActivityAt { get; }

    /// <summary>
    /// 持久化修订号。
    /// </summary>
    public long Revision { get; }

    /// <summary>
    /// 房间角色数量。
    /// </summary>
    public int RoleCount { get; }

    /// <summary>
    /// 房间公开消息数量。
    /// </summary>
    public int MessageCount { get; }
}

/// <summary>
/// 聊天室聚合仓储。
/// </summary>
public interface IChatRoomRepository
{
    /// <summary>
    /// 加载指定房间的完整一致快照。
    /// </summary>
    Task<ChatRoomSnapshot?> LoadAsync(Guid roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 以 CAS 与 CommitId 幂等语义保存完整一致快照。
    /// </summary>
    Task<ChatRoomSaveResult> SaveAsync(
        ChatRoomSaveRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可恢复房间。
    /// </summary>
    Task<IReadOnlyList<ChatRoomSummary>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定房间。
    /// </summary>
    Task DeleteAsync(Guid roomId, CancellationToken cancellationToken = default);
}
