using AgentLib.ChatRoom.Domain;

namespace AgentLib.ChatRoom.Persistence;

internal sealed class InMemoryChatRoomRepository : IChatRoomRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ChatRoomSnapshot> _snapshots = [];
    private readonly Dictionary<(Guid RoomId, Guid CommitId), long> _committedRevisions = [];
    private readonly HashSet<Guid> _tombstones = [];

    public Task<ChatRoomSnapshot?> LoadAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(
                _snapshots.TryGetValue(roomId, out ChatRoomSnapshot? snapshot)
                    ? snapshot.DeepClone()
                    : null);
        }
    }

    public Task<ChatRoomSaveResult> SaveAsync(
        ChatRoomSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        Guid roomId = request.Snapshot.State.RoomId;
        long revision = request.Snapshot.State.Revision;
        lock (_sync)
        {
            if (_tombstones.Contains(roomId))
            {
                return Task.FromResult(new ChatRoomSaveResult(
                    ChatRoomSaveStatus.Deleted,
                    roomId,
                    request.CommitId,
                    durableRevision: -1));
            }

            var commitKey = (roomId, request.CommitId);
            if (_committedRevisions.TryGetValue(commitKey, out long committedRevision))
            {
                if (committedRevision != revision)
                {
                    throw new InvalidOperationException(
                        $"CommitId {request.CommitId} 已用于房间 {roomId} 的修订 {committedRevision}，不能复用于修订 {revision}。");
                }

                return Task.FromResult(new ChatRoomSaveResult(
                    ChatRoomSaveStatus.AlreadyCommitted,
                    roomId,
                    request.CommitId,
                    committedRevision));
            }

            long durableRevision = _snapshots.TryGetValue(roomId, out ChatRoomSnapshot? current)
                ? current.State.Revision
                : -1;
            if (durableRevision != request.ExpectedRevision)
            {
                return Task.FromResult(new ChatRoomSaveResult(
                    ChatRoomSaveStatus.Conflict,
                    roomId,
                    request.CommitId,
                    durableRevision));
            }
            if (revision <= durableRevision)
            {
                throw new ArgumentException(
                    "待保存快照的修订号必须高于当前 durable revision。",
                    nameof(request));
            }

            _snapshots[roomId] = request.Snapshot.DeepClone();
            _committedRevisions.Add(commitKey, revision);
            return Task.FromResult(new ChatRoomSaveResult(
                ChatRoomSaveStatus.Saved,
                roomId,
                request.CommitId,
                revision));
        }
    }

    public Task<IReadOnlyList<ChatRoomSummary>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            IReadOnlyList<ChatRoomSummary> summaries = _snapshots.Values
                .Select(snapshot => new ChatRoomSummary(
                    snapshot.State.RoomId,
                    snapshot.State.Title,
                    snapshot.State.CreatedAt,
                    snapshot.State.LastActivityAt,
                    snapshot.State.Revision,
                    snapshot.State.Roles.Count,
                    snapshot.State.Messages.Count))
                .OrderByDescending(summary => summary.LastActivityAt)
                .ThenBy(summary => summary.RoomId)
                .ToArray();
            return Task.FromResult(summaries);
        }
    }

    public Task DeleteAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _snapshots.Remove(roomId);
            _tombstones.Add(roomId);
        }

        return Task.CompletedTask;
    }
}
