using AgentLib.ChatRoom.Coordination;
using AgentLib.ChatRoom.Domain;
using AgentLib.ChatRoom.Runtime;

using System.Threading.Channels;

namespace AgentLib.ChatRoom.Tests.Architecture;

[TestClass]
public sealed class ChatRoomCoordinatorTests
{
    [TestMethod(DisplayName = "并发命令应由单写者生成连续修订和消息序号")]
    [Timeout(5000)]
    public async Task ConcurrentCommandsShouldProduceContiguousRevisionsAndMessageSequences()
    {
        await using ChatRoomCoordinator coordinator = CreateCoordinatorWithoutRoles();
        await using ChatRoomCoordinatorSubscription subscription = coordinator.Subscribe();
        var first = new AppendHumanMessageCommand(Guid.NewGuid(), "第一条", "human", "用户");
        var second = new AppendHumanMessageCommand(Guid.NewGuid(), "第二条", "human", "用户");

        ChatRoomCommandReceipt[] receipts = await Task.WhenAll(
            coordinator.ExecuteAsync(first),
            coordinator.ExecuteAsync(second));
        ChatRoomChange[] changes = await ReadChangesAsync(subscription, count: 2);
        ChatRoomState state = coordinator.State;

        CollectionAssert.AreEquivalent(new long[] { 1, 2 }, receipts.Select(receipt => receipt.State.Revision).ToArray());
        CollectionAssert.AreEqual(new long[] { 1, 2 }, state.Messages.Select(message => message.MessageSequence).ToArray());
        CollectionAssert.AreEquivalent(new[] { "第一条", "第二条" }, state.Messages.Select(message => message.Content).ToArray());
        Assert.AreEqual(2L, state.Revision);
        Assert.AreEqual(3L, state.NextMessageSequence);
        CollectionAssert.AreEqual(new long[] { 1, 2 }, changes.Select(change => change.EventSequence).ToArray());
    }

    [TestMethod(DisplayName = "重复 CommandId 应返回原回执且不重复修改状态")]
    [Timeout(5000)]
    public async Task RepeatedCommandIdShouldBeIdempotent()
    {
        await using ChatRoomCoordinator coordinator = CreateCoordinatorWithoutRoles();
        var command = new AppendHumanMessageCommand(Guid.NewGuid(), "只追加一次", "human", "用户");

        ChatRoomCommandReceipt first = await coordinator.ExecuteAsync(command);
        ChatRoomCommandReceipt second = await coordinator.ExecuteAsync(command);

        Assert.AreSame(first, second);
        Assert.HasCount(1, coordinator.State.Messages);
        Assert.AreEqual(1L, coordinator.State.Revision);
    }

    [TestMethod(DisplayName = "订阅应原子返回初始快照和下一事件序号")]
    [Timeout(5000)]
    public async Task SubscriptionShouldAtomicallyExposeSnapshotAndNextEventSequence()
    {
        await using ChatRoomCoordinator coordinator = CreateCoordinatorWithoutRoles();
        await coordinator.ExecuteAsync(new AppendHumanMessageCommand(
            Guid.NewGuid(),
            "初始消息",
            "human",
            "用户"));
        await using ChatRoomCoordinatorSubscription subscription = coordinator.Subscribe();

        Task<ChatRoomCommandReceipt> renameTask = coordinator.ExecuteAsync(
            new RenameRoomCommand(Guid.NewGuid(), "新标题"));
        ChatRoomChange change = (await ReadChangesAsync(subscription, count: 1))[0];
        await renameTask;

        Assert.AreEqual(1L, subscription.InitialSnapshot.State.Revision);
        Assert.AreEqual(2L, subscription.NextEventSequence);
        Assert.AreEqual(2L, change.EventSequence);
        Assert.AreEqual("新标题", change.State.Title);
    }

    [TestMethod(DisplayName = "房间级同时只能有一个角色执行")]
    [Timeout(5000)]
    public async Task OnlyOneRoleExecutionShouldBeActivePerRoom()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        var first = new StartRoleExecutionCommand(Guid.NewGuid(), fixture.Definition.Identity.RoleId);
        var second = new StartRoleExecutionCommand(Guid.NewGuid(), fixture.Definition.Identity.RoleId);

        ChatRoomCommandReceipt firstReceipt = await coordinator.ExecuteAsync(first);
        ControlledRuntimeInvocation invocation = await fixture.Runtime.NextInvocationAsync();
        ChatRoomCommandReceipt secondReceipt = await coordinator.ExecuteAsync(second);

        Assert.AreEqual(ChatRoomCommandOutcome.Applied, firstReceipt.Outcome);
        Assert.AreEqual(ChatRoomCommandOutcome.Rejected, secondReceipt.Outcome);
        Assert.AreEqual(first.CommandId, coordinator.State.CurrentExecution!.ExecutionId);

        invocation.Complete("完成");
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);
    }

    [TestMethod(DisplayName = "执行请求应按当前非人类角色数量设置人类前缀省略事实")]
    [Timeout(5000)]
    public async Task ExecutionRequestShouldReflectCurrentAiRoleCount()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        await coordinator.ExecuteAsync(new AddRoleCommand(Guid.NewGuid(), new ChatRoomRoleDefinition(
            new ChatRoomRoleIdentity("human-role", incarnation: 0),
            ChatRoomRoleExecutionKind.Standard,
            "另一位人类",
            "",
            isHuman: true)));

        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(
            Guid.NewGuid(),
            fixture.Definition.Identity.RoleId));
        ControlledRuntimeInvocation singleInvocation = await fixture.Runtime.NextInvocationAsync();
        Assert.IsTrue(singleInvocation.Request.OmitHumanSenderPrefix);
        singleInvocation.Complete("单 AI 回答");
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);

        ChatRoomRoleDefinition secondDefinition = CreateDefinition("second", "第二助手", runtimeVersion: 1);
        await coordinator.ExecuteAsync(new AddRoleCommand(Guid.NewGuid(), secondDefinition));
        await coordinator.ExecuteAsync(new AppendHumanMessageCommand(
            Guid.NewGuid(),
            "新的问题",
            "human",
            "用户"));
        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(
            Guid.NewGuid(),
            fixture.Definition.Identity.RoleId));
        ControlledRuntimeInvocation multipleInvocation = await fixture.Runtime.NextInvocationAsync();

        Assert.IsFalse(multipleInvocation.Request.OmitHumanSenderPrefix);
        multipleInvocation.Complete("多 AI 回答");
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);
    }

    [TestMethod(DisplayName = "执行期间插话不应改变固定输入水位且下一轮不得跳过消息")]
    [Timeout(5000)]
    public async Task HumanInterjectionShouldNotAdvanceActiveExecutionInputWatermark()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        await using ChatRoomCoordinatorSubscription subscription = coordinator.Subscribe();
        var start = new StartRoleExecutionCommand(Guid.NewGuid(), fixture.Definition.Identity.RoleId);

        await coordinator.ExecuteAsync(start);
        ControlledRuntimeInvocation firstInvocation = await fixture.Runtime.NextInvocationAsync();
        Assert.AreEqual(1L, firstInvocation.Request.InputThroughSequence);
        await coordinator.ExecuteAsync(new AppendHumanMessageCommand(
            Guid.NewGuid(),
            "执行期间插话",
            "human",
            "用户"));

        Assert.AreEqual(1L, coordinator.State.CurrentExecution!.InputThroughSequence);
        firstInvocation.Complete("第一轮回答");
        await ReadUntilAsync(subscription, change =>
            change.ExecutionId == start.CommandId && change.State.CurrentExecution is null);

        ChatRoomState afterFirst = coordinator.State;
        Assert.AreEqual(1L, afterFirst.ConsumedThroughSequenceByRole[fixture.Definition.Identity.RoleId]);
        Assert.AreEqual(1L, coordinator.GetSnapshot().RoleCheckpoints.Single().ConsumedThroughSequence);

        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(
            Guid.NewGuid(),
            fixture.Definition.Identity.RoleId));
        ControlledRuntimeInvocation secondInvocation = await fixture.Runtime.NextInvocationAsync();

        Assert.AreEqual(3L, secondInvocation.Request.InputThroughSequence);
        CollectionAssert.AreEqual(
            new long[] { 2, 3 },
            secondInvocation.Request.InputMessages.Select(message => message.MessageSequence).ToArray());
        secondInvocation.Complete("第二轮回答");
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);
    }

    [TestMethod(DisplayName = "接受候选时公开消息、消费水位和 checkpoint 应同修订提交")]
    [Timeout(5000)]
    public async Task CandidateAcceptanceShouldAtomicallyCommitMessageWatermarkAndCheckpoint()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        var start = new StartRoleExecutionCommand(Guid.NewGuid(), fixture.Definition.Identity.RoleId);

        await coordinator.ExecuteAsync(start);
        ControlledRuntimeInvocation invocation = await fixture.Runtime.NextInvocationAsync();
        invocation.Complete("候选回答", "Fake Model");
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);

        ChatRoomSnapshot snapshot = coordinator.GetSnapshot();
        ChatRoomMessage assistantMessage = snapshot.State.Messages.Single(message =>
            message.Kind == ChatRoomMessageKind.Assistant);
        ChatRoomRoleCheckpoint checkpoint = snapshot.RoleCheckpoints.Single();

        Assert.AreEqual("候选回答", assistantMessage.Content);
        Assert.AreEqual("Fake Model", assistantMessage.ModelDisplayName);
        Assert.AreEqual(snapshot.State.Revision, checkpoint.CheckpointRevision);
        Assert.AreEqual(1L, checkpoint.ConsumedThroughSequence);
        Assert.AreEqual(1L, snapshot.State.ConsumedThroughSequenceByRole[fixture.Definition.Identity.RoleId]);
    }

    [TestMethod(DisplayName = "运行时失败应丢弃候选且不推进消费水位")]
    [Timeout(5000)]
    public async Task RuntimeFailureShouldDiscardCandidateState()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;

        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(
            Guid.NewGuid(),
            fixture.Definition.Identity.RoleId));
        ControlledRuntimeInvocation invocation = await fixture.Runtime.NextInvocationAsync();
        invocation.Fail(new InvalidOperationException("模型失败"));
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);

        ChatRoomSnapshot snapshot = coordinator.GetSnapshot();
        Assert.HasCount(1, snapshot.State.Messages);
        Assert.IsEmpty(snapshot.RoleCheckpoints);
        Assert.IsFalse(snapshot.State.ConsumedThroughSequenceByRole.ContainsKey(fixture.Definition.Identity.RoleId));
    }

    [TestMethod(DisplayName = "错误房间实例和已完成执行的迟到候选必须被拒绝")]
    [Timeout(5000)]
    public async Task StaleCompletionShouldNotMutateCurrentState()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        var start = new StartRoleExecutionCommand(Guid.NewGuid(), fixture.Definition.Identity.RoleId);
        await coordinator.ExecuteAsync(start);
        ControlledRuntimeInvocation invocation = await fixture.Runtime.NextInvocationAsync();
        ChatRoomRoleExecutionCandidate candidate = invocation.CreateCandidate("最终回答");
        long runningRevision = coordinator.State.Revision;

        ChatRoomCommandReceipt wrongRoomReceipt = await coordinator.ExecuteAsync(
            new CompleteRoleExecutionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                coordinator.State.WorkspaceVersion,
                candidate));

        Assert.AreEqual(ChatRoomCommandOutcome.Stale, wrongRoomReceipt.Outcome);
        Assert.AreEqual(runningRevision, coordinator.State.Revision);
        Assert.IsNotNull(coordinator.State.CurrentExecution);

        invocation.Complete(candidate);
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);
        long completedRevision = coordinator.State.Revision;
        ChatRoomCommandReceipt lateReceipt = await coordinator.ExecuteAsync(
            new CompleteRoleExecutionCommand(
                Guid.NewGuid(),
                coordinator.State.RoomInstanceId,
                coordinator.State.WorkspaceVersion,
                candidate));

        Assert.AreEqual(ChatRoomCommandOutcome.Stale, lateReceipt.Outcome);
        Assert.AreEqual(completedRevision, coordinator.State.Revision);
        Assert.HasCount(2, coordinator.State.Messages);
    }

    [TestMethod(DisplayName = "角色增删改应通过 runtime registry 且更新版本时清除旧 checkpoint")]
    [Timeout(5000)]
    public async Task RoleCommandsShouldCoordinateRuntimeLifecycleAndCheckpointReset()
    {
        var factory = new ControlledRuntimeFactory(ChatRoomRoleExecutionKind.Standard);
        var registry = new ChatRoomRoleRuntimeRegistry([factory]);
        await using var coordinator = new ChatRoomCoordinator(CreateSnapshot(), registry, CreateClock());
        ChatRoomRoleDefinition first = CreateDefinition(runtimeVersion: 1);

        ChatRoomCommandReceipt addReceipt = await coordinator.ExecuteAsync(
            new AddRoleCommand(Guid.NewGuid(), first));
        ControlledRoleRuntime firstRuntime = factory.Runtimes.Single();
        await coordinator.ExecuteAsync(new AppendHumanMessageCommand(
            Guid.NewGuid(),
            "问题",
            "human",
            "用户"));
        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(Guid.NewGuid(), first.Identity.RoleId));
        ControlledRuntimeInvocation invocation = await firstRuntime.NextInvocationAsync();
        invocation.Complete("回答");
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);
        Assert.HasCount(1, coordinator.GetSnapshot().RoleCheckpoints);

        ChatRoomRoleDefinition updated = CreateDefinition(runtimeVersion: 2);
        ChatRoomCommandReceipt updateReceipt = await coordinator.ExecuteAsync(
            new UpdateRoleCommand(Guid.NewGuid(), updated));

        Assert.AreEqual(ChatRoomCommandOutcome.Applied, addReceipt.Outcome);
        Assert.AreEqual(ChatRoomCommandOutcome.Applied, updateReceipt.Outcome);
        Assert.IsEmpty(coordinator.GetSnapshot().RoleCheckpoints);
        Assert.AreEqual(0L, coordinator.State.ConsumedThroughSequenceByRole[updated.Identity.RoleId]);
        Assert.AreEqual(1, firstRuntime.DisposeCount);

        await coordinator.ExecuteAsync(new RemoveRoleCommand(Guid.NewGuid(), updated.Identity.RoleId));
        Assert.IsEmpty(coordinator.State.Roles);
        Assert.AreEqual(1, factory.Runtimes[1].DisposeCount);
    }

    [TestMethod(DisplayName = "自动循环应按多 mention 文本顺序执行并遵守轮次上限")]
    [Timeout(5000)]
    public async Task AutoLoopShouldFollowMentionOrderAndTurnLimits()
    {
        ChatRoomRoleDefinition firstRole = CreateDefinition(
            "first",
            "甲",
            runtimeVersion: 1,
            ChatRoomParticipationMode.MentionOnly);
        ChatRoomRoleDefinition secondRole = CreateDefinition(
            "second",
            "乙",
            runtimeVersion: 1,
            ChatRoomParticipationMode.MentionOnly);
        var factory = new ControlledRuntimeFactory(ChatRoomRoleExecutionKind.Standard);
        var registry = new ChatRoomRoleRuntimeRegistry([factory]);
        await registry.AddAsync(firstRole);
        await registry.AddAsync(secondRole);
        var message = new ChatRoomMessage(
            1,
            Guid.NewGuid(),
            ChatRoomMessageKind.Human,
            "@甲 @乙 请依次回答",
            new DateTimeOffset(2026, 1, 1, 8, 1, 0, TimeSpan.Zero),
            "human",
            "用户",
            [firstRole.Identity.RoleId, secondRole.Identity.RoleId]);
        await using var coordinator = new ChatRoomCoordinator(
            CreateSnapshot([firstRole, secondRole], [message], revision: 1, nextMessageSequence: 2),
            registry,
            CreateClock());
        ControlledRoleRuntime firstRuntime = factory.Runtimes.Single(runtime => runtime.Identity == firstRole.Identity);
        ControlledRoleRuntime secondRuntime = factory.Runtimes.Single(runtime => runtime.Identity == secondRole.Identity);

        await coordinator.ExecuteAsync(new StartAutoLoopCommand(
            Guid.NewGuid(),
            maxTurns: 2,
            maxTurnsPerRole: 1));
        ControlledRuntimeInvocation firstInvocation = await firstRuntime.NextInvocationAsync();
        firstInvocation.Complete("甲回答");
        ControlledRuntimeInvocation secondInvocation = await secondRuntime.NextInvocationAsync();
        secondInvocation.Complete("乙回答");
        await WaitForStateAsync(coordinator, state =>
            state.AutoLoop.Status == ChatRoomAutoLoopStatus.Idle && state.CurrentExecution is null);

        CollectionAssert.AreEqual(
            new[] { "human", "first", "second" },
            coordinator.State.Messages.Select(message => message.SenderRoleId).ToArray());
        Assert.AreEqual(2, coordinator.State.AutoLoop.CompletedTurns);
    }

    [TestMethod(DisplayName = "没有普通候选时自动循环应由 manager 兜底")]
    [Timeout(5000)]
    public async Task AutoLoopShouldUseManagerFallbackWhenNoDefaultRoleCanSpeak()
    {
        ChatRoomRoleDefinition manager = CreateDefinition(
            "manager",
            "主持人",
            runtimeVersion: 1,
            ChatRoomParticipationMode.MentionOnly,
            isManagerRole: true);
        var factory = new ControlledRuntimeFactory(ChatRoomRoleExecutionKind.Standard);
        var registry = new ChatRoomRoleRuntimeRegistry([factory]);
        await registry.AddAsync(manager);
        var message = new ChatRoomMessage(
            1,
            Guid.NewGuid(),
            ChatRoomMessageKind.Human,
            "请主持讨论",
            new DateTimeOffset(2026, 1, 1, 8, 1, 0, TimeSpan.Zero),
            "human",
            "用户");
        await using var coordinator = new ChatRoomCoordinator(
            CreateSnapshot([manager], [message], revision: 1, nextMessageSequence: 2),
            registry,
            CreateClock());
        ControlledRoleRuntime managerRuntime = factory.Runtimes.Single();

        await coordinator.ExecuteAsync(new StartAutoLoopCommand(
            Guid.NewGuid(),
            maxTurns: 1,
            maxTurnsPerRole: 1));
        ControlledRuntimeInvocation invocation = await managerRuntime.NextInvocationAsync();

        Assert.AreEqual(manager.Identity, invocation.Request.Definition.Identity);
        invocation.Complete("主持人回答");
        await WaitForStateAsync(coordinator, state => state.AutoLoop.Status == ChatRoomAutoLoopStatus.Idle);
    }

    [TestMethod(DisplayName = "流式增量和审批应按 execution 与 ApprovalId 精确路由")]
    [Timeout(5000)]
    public async Task StreamingAndApprovalShouldUseExactExecutionRouting()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        await using ChatRoomCoordinatorSubscription subscription = coordinator.Subscribe();
        Guid executionId = Guid.NewGuid();
        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(
            executionId,
            fixture.Definition.Identity.RoleId));
        ControlledRuntimeInvocation invocation = await fixture.Runtime.NextInvocationAsync();
        Assert.IsNotNull(invocation.EventSink);

        await invocation.EventSink.ReportDeltaAsync(ChatRoomStreamDeltaKind.PublicText, "流式片段");
        ChatRoomExecutionEvent deltaEvent = await ReadExecutionEventAsync(subscription);
        var delta = Assert.IsInstanceOfType<ChatRoomStreamDeltaEvent>(deltaEvent).Delta;
        Assert.AreEqual(executionId, delta.ExecutionId);
        Assert.AreEqual("流式片段", delta.Content);

        Task<ChatRoomApprovalDecision> approvalTask = invocation.EventSink.RequestApprovalAsync(
            "approval-1",
            "允许工具调用");
        var approvalEvent = Assert.IsInstanceOfType<ChatRoomApprovalRequestedEvent>(
            await ReadExecutionEventAsync(subscription));
        Assert.AreEqual(ChatRoomExecutionStatus.AwaitingApproval, coordinator.State.CurrentExecution!.Status);
        ChatRoomCommandReceipt staleReceipt = await coordinator.ExecuteAsync(new RespondToApprovalCommand(
            Guid.NewGuid(),
            new ChatRoomApprovalResponse(
                coordinator.State.RoomId,
                coordinator.State.RoomInstanceId,
                executionId,
                "wrong-approval",
                ChatRoomApprovalDecision.Approved)));

        Assert.AreEqual(ChatRoomCommandOutcome.Stale, staleReceipt.Outcome);
        Assert.IsFalse(approvalTask.IsCompleted);
        await coordinator.ExecuteAsync(new RespondToApprovalCommand(
            Guid.NewGuid(),
            new ChatRoomApprovalResponse(
                coordinator.State.RoomId,
                coordinator.State.RoomInstanceId,
                executionId,
                approvalEvent.Request.ApprovalId,
                ChatRoomApprovalDecision.Rejected)));

        Assert.AreEqual(ChatRoomApprovalDecision.Rejected, await approvalTask);
        Assert.AreEqual(ChatRoomExecutionStatus.Running, coordinator.State.CurrentExecution!.Status);
        invocation.Complete("已处理拒绝结果");
        await WaitForStateAsync(coordinator, state => state.CurrentExecution is null);
    }

    [TestMethod(DisplayName = "跨 runtime 工作区发布失败应回滚所有已应用候选")]
    [Timeout(5000)]
    public async Task WorkspaceSagaShouldRollbackAllAppliedRuntimeTransactions()
    {
        ChatRoomRoleDefinition firstRole = CreateDefinition("first", "甲", 1);
        ChatRoomRoleDefinition secondRole = CreateDefinition("second", "乙", 1);
        var factory = new ControlledRuntimeFactory(ChatRoomRoleExecutionKind.Standard);
        var registry = new ChatRoomRoleRuntimeRegistry([factory]);
        await registry.AddAsync(firstRole);
        await registry.AddAsync(secondRole);
        await using var coordinator = new ChatRoomCoordinator(
            CreateSnapshot([firstRole, secondRole]),
            registry,
            CreateClock());
        ControlledRoleRuntime firstRuntime = factory.Runtimes.Single(runtime => runtime.Identity == firstRole.Identity);
        ControlledRoleRuntime secondRuntime = factory.Runtimes.Single(runtime => runtime.Identity == secondRole.Identity);

        await coordinator.ExecuteAsync(new ChangeWorkspaceCommand(Guid.NewGuid(), "workspace-one"));

        Assert.AreEqual("workspace-one", coordinator.WorkspacePath);
        Assert.AreEqual(1L, coordinator.State.WorkspaceVersion);
        Assert.AreEqual(1, firstRuntime.WorkspaceTransactions[0].CommitCount);
        Assert.AreEqual(1, secondRuntime.WorkspaceTransactions[0].CommitCount);

        secondRuntime.FailNextWorkspaceApply = true;
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => coordinator.ExecuteAsync(
            new ChangeWorkspaceCommand(Guid.NewGuid(), "workspace-two")));

        Assert.AreEqual("workspace-one", coordinator.WorkspacePath);
        Assert.AreEqual(1L, coordinator.State.WorkspaceVersion);
        Assert.AreEqual("workspace-one", firstRuntime.CurrentWorkspacePath);
        Assert.AreEqual("workspace-one", secondRuntime.CurrentWorkspacePath);
        Assert.AreEqual(1, firstRuntime.WorkspaceTransactions[1].RollbackCount);
        Assert.AreEqual(1, secondRuntime.WorkspaceTransactions[1].RollbackCount);
    }

    [TestMethod(DisplayName = "停止自动循环应取消并等待当前执行进入终态")]
    [Timeout(5000)]
    public async Task StopAutoLoopShouldCancelCurrentExecutionAndReachIdle()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        await coordinator.ExecuteAsync(new StartAutoLoopCommand(Guid.NewGuid()));
        _ = await fixture.Runtime.NextInvocationAsync();

        ChatRoomCommandReceipt receipt = await coordinator.ExecuteAsync(
            new StopAutoLoopCommand(Guid.NewGuid()));

        Assert.AreEqual(ChatRoomCommandOutcome.Applied, receipt.Outcome);
        await WaitForStateAsync(coordinator, state =>
            state.AutoLoop.Status == ChatRoomAutoLoopStatus.Idle && state.CurrentExecution is null);
        Assert.HasCount(1, coordinator.State.Messages);
    }

    [TestMethod(DisplayName = "TryClose 应等待可取消执行真实终止")]
    [Timeout(5000)]
    public async Task TryCloseShouldWaitUntilCancelableExecutionStops()
    {
        CoordinatorFixture fixture = await CreateCoordinatorWithRoleAsync();
        await using ChatRoomCoordinator coordinator = fixture.Coordinator;
        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(
            Guid.NewGuid(),
            fixture.Definition.Identity.RoleId));
        _ = await fixture.Runtime.NextInvocationAsync();

        ChatRoomCloseResult result = await coordinator.TryCloseAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(ChatRoomCloseOutcome.Closed, result.Outcome);
        Assert.AreEqual(ChatRoomLifecycleStatus.Closed, coordinator.State.LifecycleStatus);
        Assert.IsNull(coordinator.State.CurrentExecution);
    }

    [TestMethod(DisplayName = "不可取消执行应进入 Stuck 且 ForceAbandon 后迟到结果不得提交")]
    [Timeout(5000)]
    public async Task StuckExecutionShouldRequireForceAbandonAndRejectLateCompletion()
    {
        ChatRoomRoleDefinition definition = CreateDefinition(runtimeVersion: 1);
        var factory = new ControlledRuntimeFactory(
            ChatRoomRoleExecutionKind.Standard,
            ignoreCancellation: true);
        var registry = new ChatRoomRoleRuntimeRegistry([factory]);
        await registry.AddAsync(definition);
        ControlledRoleRuntime runtime = factory.Runtimes.Single();
        var initialMessage = new ChatRoomMessage(
            1,
            Guid.NewGuid(),
            ChatRoomMessageKind.Human,
            "不会响应取消",
            new DateTimeOffset(2026, 1, 1, 8, 1, 0, TimeSpan.Zero),
            "human",
            "用户");
        var coordinator = new ChatRoomCoordinator(
            CreateSnapshot([definition], [initialMessage], revision: 1, nextMessageSequence: 2),
            registry,
            CreateClock());
        await coordinator.ExecuteAsync(new StartRoleExecutionCommand(
            Guid.NewGuid(),
            definition.Identity.RoleId));
        ControlledRuntimeInvocation invocation = await runtime.NextInvocationAsync();

        ChatRoomCloseResult closeResult = await coordinator.TryCloseAsync(TimeSpan.FromMilliseconds(50));

        Assert.AreEqual(ChatRoomCloseOutcome.Stuck, closeResult.Outcome);
        Assert.AreEqual(ChatRoomLifecycleStatus.CloseFaulted, coordinator.State.LifecycleStatus);
        Assert.AreEqual(ChatRoomExecutionStatus.Stuck, coordinator.State.CurrentExecution!.Status);
        await coordinator.ExecuteAsync(new ForceAbandonRoomCommand(Guid.NewGuid()));
        Assert.AreEqual(ChatRoomLifecycleStatus.Closed, coordinator.State.LifecycleStatus);
        Assert.IsNull(coordinator.State.CurrentExecution);

        invocation.Complete("迟到回答");
        await coordinator.DisposeAsync();
        Assert.HasCount(1, coordinator.State.Messages);
    }

    private static ChatRoomCoordinator CreateCoordinatorWithoutRoles()
    {
        var registry = new ChatRoomRoleRuntimeRegistry([]);
        return new ChatRoomCoordinator(CreateSnapshot(), registry, CreateClock());
    }

    private static async Task<CoordinatorFixture> CreateCoordinatorWithRoleAsync()
    {
        ChatRoomRoleDefinition definition = CreateDefinition(runtimeVersion: 1);
        var factory = new ControlledRuntimeFactory(ChatRoomRoleExecutionKind.Standard);
        var registry = new ChatRoomRoleRuntimeRegistry([factory]);
        await registry.AddAsync(definition);
        ControlledRoleRuntime runtime = factory.Runtimes.Single();
        ChatRoomSnapshot snapshot = CreateSnapshot(
            roles: [definition],
            messages:
            [
                new ChatRoomMessage(
                    1,
                    Guid.NewGuid(),
                    ChatRoomMessageKind.Human,
                    "初始问题",
                    new DateTimeOffset(2026, 1, 1, 8, 1, 0, TimeSpan.Zero),
                    "human",
                    "用户"),
            ],
            revision: 1,
            nextMessageSequence: 2);
        var coordinator = new ChatRoomCoordinator(snapshot, registry, CreateClock());
        return new CoordinatorFixture(coordinator, runtime, definition);
    }

    private static ChatRoomSnapshot CreateSnapshot(
        IReadOnlyList<ChatRoomRoleDefinition>? roles = null,
        IReadOnlyList<ChatRoomMessage>? messages = null,
        long revision = 0,
        long nextMessageSequence = 1)
    {
        DateTimeOffset createdAt = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        ChatRoomMessage[] messageValues = messages?.ToArray() ?? [];
        DateTimeOffset lastActivityAt = messageValues.Length == 0 ? createdAt : messageValues[^1].Timestamp;
        var state = new ChatRoomState(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "测试聊天室",
            createdAt,
            lastActivityAt,
            revision,
            durableRevision: revision,
            nextMessageSequence,
            workspaceVersion: 0,
            roles,
            messageValues);
        return new ChatRoomSnapshot(state);
    }

    private static ChatRoomRoleDefinition CreateDefinition(long runtimeVersion) =>
        CreateDefinition("assistant", "助手", runtimeVersion);

    private static ChatRoomRoleDefinition CreateDefinition(
        string roleId,
        string roleName,
        long runtimeVersion,
        ChatRoomParticipationMode participationMode = ChatRoomParticipationMode.AlwaysParticipate,
        bool isManagerRole = false) => new(
        new ChatRoomRoleIdentity(roleId, incarnation: 0),
        ChatRoomRoleExecutionKind.Standard,
        roleName,
        "系统提示词",
        isHuman: false,
        participationMode: participationMode,
        isManagerRole: isManagerRole,
        runtimeVersion: runtimeVersion);

    private static Func<DateTimeOffset> CreateClock()
    {
        long ticks = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero).Ticks;
        return () => new DateTimeOffset(Interlocked.Add(ref ticks, TimeSpan.TicksPerSecond), TimeSpan.Zero);
    }

    private static async Task<ChatRoomChange[]> ReadChangesAsync(
        ChatRoomCoordinatorSubscription subscription,
        int count)
    {
        var changes = new List<ChatRoomChange>(count);
        await using IAsyncEnumerator<ChatRoomChange> enumerator = subscription.ReadAllAsync().GetAsyncEnumerator();
        while (changes.Count < count)
        {
            Assert.IsTrue(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1)));
            changes.Add(enumerator.Current);
        }

        return changes.ToArray();
    }

    private static async Task<ChatRoomChange> ReadUntilAsync(
        ChatRoomCoordinatorSubscription subscription,
        Func<ChatRoomChange, bool> predicate)
    {
        await using IAsyncEnumerator<ChatRoomChange> enumerator = subscription.ReadAllAsync().GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1)))
        {
            if (predicate(enumerator.Current))
            {
                return enumerator.Current;
            }
        }

        throw new AssertFailedException("事件流在目标状态前已结束。");
    }

    private static async Task<ChatRoomExecutionEvent> ReadExecutionEventAsync(
        ChatRoomCoordinatorSubscription subscription)
    {
        await using IAsyncEnumerator<ChatRoomExecutionEvent> enumerator = subscription
            .ReadExecutionEventsAsync()
            .GetAsyncEnumerator();
        Assert.IsTrue(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1)));
        return enumerator.Current;
    }

    private static async Task WaitForStateAsync(
        ChatRoomCoordinator coordinator,
        Func<ChatRoomState, bool> predicate)
    {
        if (predicate(coordinator.State))
        {
            return;
        }

        await using ChatRoomCoordinatorSubscription subscription = coordinator.Subscribe();
        if (predicate(subscription.InitialSnapshot.State))
        {
            return;
        }

        await ReadUntilAsync(subscription, change => predicate(change.State));
    }

    private sealed record CoordinatorFixture(
        ChatRoomCoordinator Coordinator,
        ControlledRoleRuntime Runtime,
        ChatRoomRoleDefinition Definition);

    private sealed class ControlledRuntimeFactory : IChatRoomRoleRuntimeFactory
    {
        private readonly bool _ignoreCancellation;

        internal ControlledRuntimeFactory(
            ChatRoomRoleExecutionKind executionKind,
            bool ignoreCancellation = false)
        {
            ExecutionKind = executionKind;
            _ignoreCancellation = ignoreCancellation;
        }

        public ChatRoomRoleExecutionKind ExecutionKind { get; }

        public List<ControlledRoleRuntime> Runtimes { get; } = [];

        public Task<IChatRoomRoleRuntime> CreateAsync(
            ChatRoomRoleDefinition definition,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var runtime = new ControlledRoleRuntime(definition, _ignoreCancellation);
            Runtimes.Add(runtime);
            return Task.FromResult<IChatRoomRoleRuntime>(runtime);
        }
    }

    private sealed class ControlledRoleRuntime(
        ChatRoomRoleDefinition definition,
        bool ignoreCancellation) : IChatRoomRoleRuntime
    {
        private readonly Channel<ControlledRuntimeInvocation> _invocations = Channel.CreateUnbounded<ControlledRuntimeInvocation>();

        public int DisposeCount { get; private set; }

        public bool FailNextWorkspaceApply { get; set; }

        public string? CurrentWorkspacePath { get; internal set; }

        public List<ControlledWorkspaceTransaction> WorkspaceTransactions { get; } = [];

        public ChatRoomRoleIdentity Identity { get; } = definition.Identity;

        public ChatRoomRoleExecutionKind ExecutionKind { get; } = definition.ExecutionKind;

        public long RuntimeVersion { get; } = definition.RuntimeVersion;

        public async Task<ChatRoomRoleExecutionCandidate> ExecuteAsync(
            ChatRoomRoleExecutionRequest request,
            IChatRoomRoleExecutionEventSink? eventSink,
            CancellationToken cancellationToken)
        {
            var invocation = new ControlledRuntimeInvocation(request, eventSink);
            await _invocations.Writer.WriteAsync(invocation, cancellationToken);
            return ignoreCancellation
                ? await invocation.Completion.Task
                : await invocation.Completion.Task.WaitAsync(cancellationToken);
        }

        public Task<IChatRoomRoleWorkspaceTransaction> PrepareWorkspaceChangeAsync(
            string? workspacePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var transaction = new ControlledWorkspaceTransaction(this, workspacePath);
            WorkspaceTransactions.Add(transaction);
            return Task.FromResult<IChatRoomRoleWorkspaceTransaction>(transaction);
        }

        public Task<ControlledRuntimeInvocation> NextInvocationAsync() =>
            _invocations.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            _invocations.Writer.TryComplete();
            return default;
        }
    }

    private sealed class ControlledWorkspaceTransaction : IChatRoomRoleWorkspaceTransaction
    {
        private readonly ControlledRoleRuntime _owner;
        private readonly string? _previousWorkspacePath;
        private ControlledWorkspaceTransactionState _state;

        internal ControlledWorkspaceTransaction(ControlledRoleRuntime owner, string? workspacePath)
        {
            _owner = owner;
            _previousWorkspacePath = owner.CurrentWorkspacePath;
            WorkspacePath = workspacePath;
        }

        public string? WorkspacePath { get; }

        public int ApplyCount { get; private set; }

        public int RollbackCount { get; private set; }

        public int CommitCount { get; private set; }

        public void Apply()
        {
            ApplyCount++;
            if (_owner.FailNextWorkspaceApply)
            {
                _owner.FailNextWorkspaceApply = false;
                throw new InvalidOperationException("工作区应用失败");
            }

            _owner.CurrentWorkspacePath = WorkspacePath;
            _state = ControlledWorkspaceTransactionState.Applied;
        }

        public ValueTask RollbackAsync()
        {
            RollbackCount++;
            if (_state == ControlledWorkspaceTransactionState.Applied)
            {
                _owner.CurrentWorkspacePath = _previousWorkspacePath;
            }
            _state = ControlledWorkspaceTransactionState.RolledBack;
            return default;
        }

        public void CommitAfterPublish()
        {
            CommitCount++;
            _state = ControlledWorkspaceTransactionState.Committed;
        }

        public ValueTask DisposeAsync() =>
            _state is ControlledWorkspaceTransactionState.Prepared or ControlledWorkspaceTransactionState.Applied
                ? RollbackAsync()
                : default;

        private enum ControlledWorkspaceTransactionState
        {
            Prepared,
            Applied,
            RolledBack,
            Committed,
        }
    }

    private sealed class ControlledRuntimeInvocation(
        ChatRoomRoleExecutionRequest request,
        IChatRoomRoleExecutionEventSink? eventSink)
    {
        public ChatRoomRoleExecutionRequest Request { get; } = request;

        public IChatRoomRoleExecutionEventSink? EventSink { get; } = eventSink;

        public TaskCompletionSource<ChatRoomRoleExecutionCandidate> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ChatRoomRoleExecutionCandidate CreateCandidate(
            string? content,
            string? modelDisplayName = null)
        {
            var checkpoint = new ChatRoomRoleCheckpointCandidate(
                Request.Definition.Identity,
                Request.Definition.RuntimeVersion,
                Request.Definition.ExecutionKind,
                sessionRevision: (Request.CommittedCheckpoint?.SessionRevision ?? 0) + 1,
                consumedThroughSequence: Request.InputThroughSequence,
                serializerVersion: 1,
                ChatRoomRoleCheckpointFormats.AgentSessionJsonV1,
                new byte[] { 1, 2, 3 });
            return new ChatRoomRoleExecutionCandidate(
                Request.ExecutionId,
                Request.Definition.Identity,
                content,
                modelDisplayName,
                checkpoint);
        }

        public void Complete(string? content, string? modelDisplayName = null) =>
            Completion.TrySetResult(CreateCandidate(content, modelDisplayName));

        public void Complete(ChatRoomRoleExecutionCandidate candidate) =>
            Completion.TrySetResult(candidate);

        public void Fail(Exception exception) => Completion.TrySetException(exception);
    }
}
