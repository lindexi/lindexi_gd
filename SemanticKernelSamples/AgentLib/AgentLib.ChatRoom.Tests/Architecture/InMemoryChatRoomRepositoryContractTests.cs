using AgentLib.ChatRoom.Domain;
using AgentLib.ChatRoom.Persistence;

namespace AgentLib.ChatRoom.Tests.Architecture;

[TestClass]
public sealed class InMemoryChatRoomRepositoryContractTests
{
    [TestMethod(DisplayName = "保存后加载应返回隔离的完整快照")]
    [Timeout(5000)]
    public async Task SaveThenLoadShouldReturnIsolatedCompleteSnapshot()
    {
        IChatRoomRepository repository = new InMemoryChatRoomRepository();
        ChatRoomSnapshot original = new ChatRoomScenarioBuilder()
            .WithRevision(1)
            .WithRole()
            .WithMessage("你好")
            .WithConsumedSequence("assistant", 1)
            .WithCheckpoint()
            .Build();
        var request = new ChatRoomSaveRequest(original, Guid.NewGuid(), expectedRevision: -1);

        ChatRoomSaveResult saveResult = await repository.SaveAsync(request);
        ChatRoomSnapshot? firstLoad = await repository.LoadAsync(original.State.RoomId);
        ChatRoomSnapshot? secondLoad = await repository.LoadAsync(original.State.RoomId);

        Assert.AreEqual(ChatRoomSaveStatus.Saved, saveResult.Status);
        Assert.IsNotNull(firstLoad);
        Assert.IsNotNull(secondLoad);
        Assert.AreNotSame(original, firstLoad);
        Assert.AreNotSame(firstLoad, secondLoad);
        Assert.AreEqual("你好", firstLoad.State.Messages[0].Content);
        Assert.AreEqual(1L, firstLoad.State.ConsumedThroughSequenceByRole["assistant"]);
        Assert.HasCount(1, firstLoad.RoleCheckpoints);
    }

    [TestMethod(DisplayName = "相同 CommitId 重试应返回已提交且不产生新修订")]
    [Timeout(5000)]
    public async Task RepeatedCommitIdShouldReturnAlreadyCommitted()
    {
        IChatRoomRepository repository = new InMemoryChatRoomRepository();
        ChatRoomSnapshot snapshot = new ChatRoomScenarioBuilder()
            .WithRevision(1)
            .Build();
        var commitId = Guid.NewGuid();
        var request = new ChatRoomSaveRequest(snapshot, commitId, expectedRevision: -1);

        ChatRoomSaveResult first = await repository.SaveAsync(request);
        ChatRoomSaveResult second = await repository.SaveAsync(request);

        Assert.AreEqual(ChatRoomSaveStatus.Saved, first.Status);
        Assert.AreEqual(ChatRoomSaveStatus.AlreadyCommitted, second.Status);
        Assert.AreEqual(1L, second.DurableRevision);
    }

    [TestMethod(DisplayName = "相同 CommitId 不得复用于不同修订")]
    [Timeout(5000)]
    public async Task CommitIdShouldNotBeReusableForDifferentRevision()
    {
        IChatRoomRepository repository = new InMemoryChatRoomRepository();
        ChatRoomSnapshot revisionOne = new ChatRoomScenarioBuilder()
            .WithRevision(1)
            .Build();
        var commitId = Guid.NewGuid();
        await repository.SaveAsync(new ChatRoomSaveRequest(revisionOne, commitId, expectedRevision: -1));
        ChatRoomSnapshot revisionTwo = CloneWithRevision(revisionOne, revision: 2);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => repository.SaveAsync(
            new ChatRoomSaveRequest(revisionTwo, commitId, expectedRevision: 1)));
    }

    [TestMethod(DisplayName = "CAS 冲突应保留当前 durable revision")]
    [Timeout(5000)]
    public async Task CasConflictShouldKeepCurrentDurableRevision()
    {
        IChatRoomRepository repository = new InMemoryChatRoomRepository();
        ChatRoomSnapshot revisionOne = new ChatRoomScenarioBuilder()
            .WithRevision(1)
            .Build();
        await repository.SaveAsync(new ChatRoomSaveRequest(revisionOne, Guid.NewGuid(), expectedRevision: -1));
        ChatRoomSnapshot revisionTwo = CloneWithRevision(revisionOne, revision: 2);

        ChatRoomSaveResult result = await repository.SaveAsync(
            new ChatRoomSaveRequest(revisionTwo, Guid.NewGuid(), expectedRevision: 0));
        ChatRoomSnapshot? loaded = await repository.LoadAsync(revisionOne.State.RoomId);

        Assert.AreEqual(ChatRoomSaveStatus.Conflict, result.Status);
        Assert.AreEqual(1L, result.DurableRevision);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1L, loaded.State.Revision);
    }

    [TestMethod(DisplayName = "列表应按最后活动时间倒序并在删除后移除房间")]
    [Timeout(5000)]
    public async Task ListAndDeleteShouldExposeOnlyCurrentSnapshots()
    {
        IChatRoomRepository repository = new InMemoryChatRoomRepository();
        ChatRoomSnapshot first = new ChatRoomScenarioBuilder()
            .WithRevision(1)
            .WithTitle("第一间")
            .Build();
        ChatRoomSnapshot second = new ChatRoomScenarioBuilder()
            .WithRevision(1)
            .WithTitle("第二间")
            .WithMessage("更新")
            .Build();
        await repository.SaveAsync(new ChatRoomSaveRequest(first, Guid.NewGuid(), expectedRevision: -1));
        await repository.SaveAsync(new ChatRoomSaveRequest(second, Guid.NewGuid(), expectedRevision: -1));

        IReadOnlyList<ChatRoomSummary> beforeDelete = await repository.ListAsync();
        await repository.DeleteAsync(second.State.RoomId);
        IReadOnlyList<ChatRoomSummary> afterDelete = await repository.ListAsync();

        Assert.HasCount(2, beforeDelete);
        Assert.AreEqual(second.State.RoomId, beforeDelete[0].RoomId);
        Assert.AreEqual(second.State.Roles.Count, beforeDelete[0].RoleCount);
        Assert.AreEqual(second.State.Messages.Count, beforeDelete[0].MessageCount);
        Assert.HasCount(1, afterDelete);
        Assert.AreEqual(first.State.RoomId, afterDelete[0].RoomId);
    }

    [TestMethod(DisplayName = "删除墓碑应拒绝迟到提交复活房间")]
    [Timeout(5000)]
    public async Task DeleteTombstoneShouldRejectLateCommit()
    {
        IChatRoomRepository repository = new InMemoryChatRoomRepository();
        ChatRoomSnapshot revisionOne = new ChatRoomScenarioBuilder()
            .WithRevision(1)
            .Build();
        await repository.SaveAsync(new ChatRoomSaveRequest(revisionOne, Guid.NewGuid(), expectedRevision: -1));
        await repository.DeleteAsync(revisionOne.State.RoomId);
        ChatRoomSnapshot revisionTwo = CloneWithRevision(revisionOne, revision: 2);

        ChatRoomSaveResult result = await repository.SaveAsync(
            new ChatRoomSaveRequest(revisionTwo, Guid.NewGuid(), expectedRevision: 1));

        Assert.AreEqual(ChatRoomSaveStatus.Deleted, result.Status);
        Assert.IsNull(await repository.LoadAsync(revisionOne.State.RoomId));
    }

    private static ChatRoomSnapshot CloneWithRevision(ChatRoomSnapshot snapshot, long revision)
    {
        ChatRoomState state = snapshot.State;
        return new ChatRoomSnapshot(new ChatRoomState(
            state.RoomId,
            state.RoomInstanceId,
            state.Title,
            state.CreatedAt,
            state.LastActivityAt,
            revision,
            state.DurableRevision > revision ? revision : state.DurableRevision,
            state.NextMessageSequence,
            state.WorkspaceVersion,
            state.Roles,
            state.Messages,
            state.ConsumedThroughSequenceByRole,
            state.CurrentExecution,
            state.PersistenceHealth,
            state.LifecycleStatus,
            state.LastPersistenceError), snapshot.RoleCheckpoints.Select(checkpoint =>
                new ChatRoomRoleCheckpoint(
                    checkpoint.RoleIdentity,
                    checkpoint.RoleRuntimeVersion,
                    checkpoint.ExecutionKind,
                    revision,
                    checkpoint.SessionRevision,
                    checkpoint.ConsumedThroughSequence,
                    checkpoint.SerializerVersion,
                    checkpoint.Format,
                    checkpoint.Payload)));
    }
}
