using AgentLib.ChatRoom.Domain;
using AgentLib.ChatRoom.Persistence;

namespace AgentLib.ChatRoom.Tests.Architecture;

[TestClass]
public sealed class ChatRoomSnapshotMapperTests
{
    [TestMethod(DisplayName = "存储映射往返应保留公开状态和 committed checkpoint")]
    [Timeout(5000)]
    public void StoredRoundTripShouldPreservePublicStateAndCommittedCheckpoint()
    {
        ChatRoomSnapshot original = new ChatRoomScenarioBuilder()
            .WithRevision(5)
            .WithWorkspaceVersion(7)
            .WithRole(executionKind: ChatRoomRoleExecutionKind.Coding, runtimeVersion: 3)
            .WithMessage("实现功能")
            .WithConsumedSequence("assistant", 1)
            .WithCheckpoint(
                executionKind: ChatRoomRoleExecutionKind.Coding,
                roleRuntimeVersion: 3,
                sessionRevision: 9,
                serializerVersion: 2)
            .Build();

        StoredChatRoomSnapshot stored = ChatRoomSnapshotMapper.ToStored(original);
        ChatRoomSnapshot restored = ChatRoomSnapshotMapper.FromStored(stored);

        Assert.AreEqual(original.State.RoomId, restored.State.RoomId);
        Assert.AreEqual(original.State.Revision, restored.State.Revision);
        Assert.AreEqual(original.State.Messages[0].Content, restored.State.Messages[0].Content);
        Assert.AreEqual(original.State.Roles[0].ExecutionKind, restored.State.Roles[0].ExecutionKind);
        Assert.AreEqual(3L, restored.State.Roles[0].RuntimeVersion);
        Assert.AreEqual(1L, restored.State.ConsumedThroughSequenceByRole["assistant"]);
        Assert.AreEqual(9L, restored.RoleCheckpoints[0].SessionRevision);
        Assert.AreEqual(1L, restored.RoleCheckpoints[0].ConsumedThroughSequence);
        Assert.AreEqual(2, restored.RoleCheckpoints[0].SerializerVersion);
        CollectionAssert.AreEqual(
            original.RoleCheckpoints[0].Payload.ToArray(),
            restored.RoleCheckpoints[0].Payload.ToArray());
    }

    [TestMethod(DisplayName = "JSON 序列化往返应使用专用存储模型")]
    [Timeout(5000)]
    public void JsonRoundTripShouldUseDedicatedStoredModel()
    {
        ChatRoomSnapshot original = new ChatRoomScenarioBuilder()
            .WithRevision(3)
            .WithRole()
            .WithMessage("持久化")
            .WithCheckpoint()
            .Build();

        byte[] payload = ChatRoomSnapshotSerializer.Serialize(original);
        ChatRoomSnapshot restored = ChatRoomSnapshotSerializer.Deserialize(payload);
        string json = System.Text.Encoding.UTF8.GetString(payload);

        Assert.AreEqual(original.State.RoomId, restored.State.RoomId);
        Assert.AreEqual(original.State.Revision, restored.State.Revision);
        StringAssert.Contains(json, "SchemaVersion");
        Assert.DoesNotContain("persistenceHealth", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("currentExecution", json, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod(DisplayName = "存储映射不应持久化瞬态执行和健康状态")]
    [Timeout(5000)]
    public void StoredRoundTripShouldResetTransientExecutionAndHealthState()
    {
        ChatRoomSnapshot original = new ChatRoomScenarioBuilder()
            .WithRevision(2)
            .WithRole()
            .WithMessage("执行中")
            .WithCurrentExecution()
            .WithPersistenceFault("磁盘不可用")
            .WithLifecycle(ChatRoomLifecycleStatus.Closing)
            .Build();

        ChatRoomSnapshot restored = ChatRoomSnapshotMapper.FromStored(
            ChatRoomSnapshotMapper.ToStored(original));

        Assert.IsNull(restored.State.CurrentExecution);
        Assert.AreNotEqual(original.State.RoomInstanceId, restored.State.RoomInstanceId);
        Assert.AreEqual(restored.State.Revision, restored.State.DurableRevision);
        Assert.AreEqual(0L, restored.State.WorkspaceVersion);
        Assert.AreEqual(ChatRoomPersistenceHealth.Clean, restored.State.PersistenceHealth);
        Assert.AreEqual(ChatRoomLifecycleStatus.Open, restored.State.LifecycleStatus);
        Assert.IsNull(restored.State.LastPersistenceError);
    }

    [TestMethod(DisplayName = "存储映射应拒绝未知 schema 版本")]
    [Timeout(5000)]
    public void StoredRoundTripShouldRejectUnknownSchemaVersion()
    {
        var stored = new StoredChatRoomSnapshot
        {
            SchemaVersion = ChatRoomSnapshotMapper.CurrentSchemaVersion + 1,
        };

        _ = Assert.ThrowsExactly<InvalidDataException>(() => ChatRoomSnapshotMapper.FromStored(stored));
    }
}
