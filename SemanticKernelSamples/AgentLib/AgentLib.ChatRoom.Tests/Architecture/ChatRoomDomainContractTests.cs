using AgentLib.ChatRoom.Coordination;
using AgentLib.ChatRoom.Domain;

namespace AgentLib.ChatRoom.Tests.Architecture;

[TestClass]
public sealed class ChatRoomDomainContractTests
{
    [TestMethod(DisplayName = "状态构造后不应受源集合修改影响")]
    [Timeout(5000)]
    public void StateShouldNotChangeWhenSourceCollectionsAreModified()
    {
        var roles = new List<ChatRoomRoleDefinition>
        {
            CreateRole("assistant", "助手"),
        };
        var messages = new List<ChatRoomMessage>
        {
            CreateMessage(1, "你好"),
        };
        var consumedSequences = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["assistant"] = 1,
        };
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        var state = new ChatRoomState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "聊天室",
            createdAt,
            messages[0].Timestamp,
            revision: 1,
            durableRevision: 0,
            nextMessageSequence: 2,
            workspaceVersion: 0,
            roles,
            messages,
            consumedSequences);

        roles.Clear();
        messages.Clear();
        consumedSequences["assistant"] = 0;

        Assert.HasCount(1, state.Roles);
        Assert.HasCount(1, state.Messages);
        Assert.AreEqual(1L, state.ConsumedThroughSequenceByRole["assistant"]);
    }

    [TestMethod(DisplayName = "状态应拒绝不连续的消息序号")]
    [Timeout(5000)]
    public void StateShouldRejectNonContiguousMessageSequences()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        var message = new ChatRoomMessage(
            2,
            Guid.NewGuid(),
            ChatRoomMessageKind.Human,
            "跳号消息",
            createdAt.AddMinutes(1),
            "human",
            "用户");

        _ = Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "聊天室",
            createdAt,
            message.Timestamp,
            revision: 1,
            durableRevision: 0,
            nextMessageSequence: 3,
            workspaceVersion: 0,
            messages: [message]));
    }

    [TestMethod(DisplayName = "相同逻辑角色的不同 incarnation 不应同时存在")]
    [Timeout(5000)]
    public void StateShouldRejectMultipleIncarnationsOfSameLogicalRole()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        ChatRoomRoleDefinition[] roles =
        [
            CreateRole("assistant", "助手一", incarnation: 0),
            CreateRole("assistant", "助手二", incarnation: 1),
        ];

        _ = Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "聊天室",
            createdAt,
            createdAt,
            revision: 0,
            durableRevision: -1,
            nextMessageSequence: 1,
            workspaceVersion: 0,
            roles));
    }

    [TestMethod(DisplayName = "Checkpoint 载荷读取和提交后应保持隔离")]
    [Timeout(5000)]
    public void CheckpointPayloadShouldBeIsolatedAcrossCandidateAndCommit()
    {
        byte[] payload = [1, 2, 3];
        var candidate = new ChatRoomRoleCheckpointCandidate(
            new ChatRoomRoleIdentity("assistant", 0),
            roleRuntimeVersion: 2,
            ChatRoomRoleExecutionKind.Standard,
            sessionRevision: 3,
            consumedThroughSequence: 1,
            serializerVersion: 1,
            "application/json",
            payload);

        payload[0] = 9;
        byte[] candidateRead = candidate.Payload.ToArray();
        candidateRead[1] = 8;
        ChatRoomRoleCheckpoint checkpoint = candidate.Commit(4);
        byte[] checkpointRead = checkpoint.Payload.ToArray();
        checkpointRead[2] = 7;

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, candidate.Payload.ToArray());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, checkpoint.Payload.ToArray());
        Assert.AreEqual(4L, checkpoint.CheckpointRevision);
        Assert.AreEqual(2L, checkpoint.RoleRuntimeVersion);
        Assert.AreEqual(3L, checkpoint.SessionRevision);
        Assert.AreEqual(1L, checkpoint.ConsumedThroughSequence);
        Assert.AreEqual(1, checkpoint.SerializerVersion);
    }

    [TestMethod(DisplayName = "深快照副本不应保留集合和载荷引用")]
    [Timeout(5000)]
    public void DeepCloneShouldNotRetainCollectionOrPayloadReferences()
    {
        ChatRoomSnapshot snapshot = new ChatRoomScenarioBuilder()
            .WithRevision(3)
            .WithRole()
            .WithMessage("问题")
            .WithConsumedSequence("assistant", 1)
            .WithCheckpoint()
            .Build();

        ChatRoomSnapshot clone = snapshot.DeepClone();

        Assert.AreNotSame(snapshot, clone);
        Assert.AreNotSame(snapshot.State, clone.State);
        Assert.AreNotSame(snapshot.State.Roles, clone.State.Roles);
        Assert.AreNotSame(snapshot.State.Messages, clone.State.Messages);
        Assert.AreNotSame(snapshot.State.ConsumedThroughSequenceByRole, clone.State.ConsumedThroughSequenceByRole);
        Assert.AreNotSame(snapshot.RoleCheckpoints, clone.RoleCheckpoints);
        CollectionAssert.AreEqual(
            snapshot.RoleCheckpoints[0].Payload.ToArray(),
            clone.RoleCheckpoints[0].Payload.ToArray());
    }

    [TestMethod(DisplayName = "状态应拒绝无效实例、耐久修订和过期执行版本")]
    [Timeout(5000)]
    public void StateShouldValidateInstanceAndVersionFacts()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        ChatRoomRoleDefinition role = CreateRole("assistant", "助手", runtimeVersion: 2);
        var execution = new ChatRoomExecutionState(
            Guid.NewGuid(),
            role.Identity,
            roleRuntimeVersion: 1,
            workspaceVersion: 3,
            inputThroughSequence: 0,
            ChatRoomExecutionStatus.Running,
            createdAt);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ChatRoomState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "聊天室",
            createdAt,
            createdAt,
            revision: 2,
            durableRevision: 3,
            nextMessageSequence: 1,
            workspaceVersion: 3,
            roles: [role]));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "聊天室",
            createdAt,
            createdAt,
            revision: 2,
            durableRevision: 2,
            nextMessageSequence: 1,
            workspaceVersion: 3,
            roles: [role],
            currentExecution: execution));
    }

    [TestMethod(DisplayName = "事件、流式增量和审批 DTO 应携带精确路由身份")]
    [Timeout(5000)]
    public void CoordinationContractsShouldCarryExactRoutingIdentity()
    {
        ChatRoomState state = new ChatRoomScenarioBuilder().WithRevision(1).Build().State;
        var change = new ChatRoomChange(ChatRoomChangeKind.RoomUpdated, state, eventSequence: 4, DateTimeOffset.UtcNow);
        Guid executionId = Guid.NewGuid();
        var delta = new ChatRoomStreamDelta(
            state.RoomId,
            state.RoomInstanceId,
            eventSequence: 5,
            executionId,
            "assistant",
            ChatRoomStreamDeltaKind.PublicText,
            "增量");
        var approval = new ChatRoomApprovalRequest(
            state.RoomId,
            state.RoomInstanceId,
            6,
            executionId,
            "approval-1",
            "assistant",
            "允许执行工具");
        var response = new ChatRoomApprovalResponse(
            state.RoomId,
            state.RoomInstanceId,
            executionId,
            approval.ApprovalId,
            ChatRoomApprovalDecision.Approved);

        Assert.AreEqual(state.RoomId, change.RoomId);
        Assert.AreEqual(state.RoomInstanceId, change.RoomInstanceId);
        Assert.AreEqual(4L, change.EventSequence);
        Assert.AreEqual(executionId, delta.ExecutionId);
        Assert.AreEqual(approval.ApprovalId, response.ApprovalId);
        Assert.AreEqual(ChatRoomApprovalDecision.Approved, response.Decision);
    }

    private static ChatRoomRoleDefinition CreateRole(
        string roleId,
        string roleName,
        long incarnation = 0,
        long runtimeVersion = 0) => new(
        new ChatRoomRoleIdentity(roleId, incarnation),
        ChatRoomRoleExecutionKind.Standard,
        roleName,
        "系统提示词",
        isHuman: false,
        runtimeVersion: runtimeVersion);

    private static ChatRoomMessage CreateMessage(long sequence, string content)
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow.AddMinutes(sequence);
        return new ChatRoomMessage(
            sequence,
            Guid.NewGuid(),
            ChatRoomMessageKind.Human,
            content,
            timestamp,
            "human",
            "用户");
    }
}
