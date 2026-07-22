using AgentLib.ChatRoom.Domain;
using AgentLib.ChatRoom.Runtime;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.ChatRoom.Tests.Architecture;

[TestClass]
public sealed class ChatRoomRoleRuntimeTests
{
    [TestMethod(DisplayName = "Standard 和 Coding 工厂应创建匹配定义事实的隔离运行时")]
    [Timeout(5000)]
    public async Task RuntimeFactoriesShouldCreateMatchingIsolatedRuntime()
    {
        var roleFactory = new ChatRoomRoleFactory();
        var providerSnapshot = new ChatRoomModelProviderSnapshot(
            new Dictionary<string, ILanguageModelProvider>());
        var standardFactory = new ChatRoomRoleRuntimeFactory(
            ChatRoomRoleExecutionKind.Standard,
            roleFactory,
            providerSnapshot);
        var codingFactory = new ChatRoomRoleRuntimeFactory(
            ChatRoomRoleExecutionKind.Coding,
            roleFactory,
            providerSnapshot);
        ChatRoomRoleDefinition standardDefinition = CreateDefinition(
            "standard",
            ChatRoomRoleExecutionKind.Standard,
            runtimeVersion: 3);
        ChatRoomRoleDefinition codingDefinition = CreateDefinition(
            "coding",
            ChatRoomRoleExecutionKind.Coding,
            runtimeVersion: 7);

        await using IChatRoomRoleRuntime standard = await standardFactory.CreateAsync(
            standardDefinition,
            CancellationToken.None);
        await using IChatRoomRoleRuntime coding = await codingFactory.CreateAsync(
            codingDefinition,
            CancellationToken.None);

        Assert.AreEqual(standardDefinition.Identity, standard.Identity);
        Assert.AreEqual(3L, standard.RuntimeVersion);
        Assert.AreEqual(ChatRoomRoleExecutionKind.Standard, standard.ExecutionKind);
        Assert.AreEqual(codingDefinition.Identity, coding.Identity);
        Assert.AreEqual(7L, coding.RuntimeVersion);
        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, coding.ExecutionKind);
    }

    [TestMethod(DisplayName = "模型提供商快照应与源字典修改隔离")]
    [Timeout(5000)]
    public void ProviderSnapshotShouldBeIsolatedFromSourceDictionary()
    {
        var provider = new Moq.Mock<ILanguageModelProvider>().Object;
        var source = new Dictionary<string, ILanguageModelProvider>(StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = provider,
        };
        var snapshot = new ChatRoomModelProviderSnapshot(source, "model");

        source.Clear();

        Assert.HasCount(1, snapshot.Providers);
        Assert.AreSame(provider, snapshot.Providers["provider"]);
        Assert.AreEqual("model", snapshot.DefaultPrimaryModelId);
    }

    [TestMethod(DisplayName = "执行请求应拒绝 checkpoint 运行时版本和消费水位不一致")]
    [Timeout(5000)]
    public void ExecutionRequestShouldRejectStaleOrFutureCheckpoint()
    {
        ChatRoomRoleDefinition definition = CreateDefinition(
            "assistant",
            ChatRoomRoleExecutionKind.Standard,
            runtimeVersion: 2);
        ChatRoomMessage message = CreateHumanMessage();
        var staleCheckpoint = new ChatRoomRoleCheckpoint(
            definition.Identity,
            roleRuntimeVersion: 1,
            definition.ExecutionKind,
            checkpointRevision: 1,
            sessionRevision: 1,
            consumedThroughSequence: 1,
            serializerVersion: 1,
            ChatRoomRoleCheckpointFormats.AgentSessionJsonV1,
            new byte[] { 1 });
        var futureCheckpoint = new ChatRoomRoleCheckpoint(
            definition.Identity,
            definition.RuntimeVersion,
            definition.ExecutionKind,
            checkpointRevision: 1,
            sessionRevision: 1,
            consumedThroughSequence: 2,
            serializerVersion: 1,
            ChatRoomRoleCheckpointFormats.AgentSessionJsonV1,
            new byte[] { 1 });

        _ = Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomRoleExecutionRequest(
            Guid.NewGuid(),
            definition,
            [message],
            inputThroughSequence: 1,
            staleCheckpoint,
            workspacePath: null,
            omitHumanSenderPrefix: true));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomRoleExecutionRequest(
            Guid.NewGuid(),
            definition,
            [message],
            inputThroughSequence: 1,
            futureCheckpoint,
            workspacePath: null,
            omitHumanSenderPrefix: true));
    }

    [TestMethod(DisplayName = "隔离运行时应按执行事实格式化人类消息并始终标注 AI 消息")]
    public void IsolatedRuntimeShouldFormatIncrementalMessagesFromExecutionFact()
    {
        ChatRoomMessage human = CreateHumanMessage();
        var assistant = new ChatRoomMessage(
            2,
            Guid.NewGuid(),
            ChatRoomMessageKind.Assistant,
            "历史回答",
            DateTimeOffset.UtcNow,
            "other",
            "其他助手");
        var own = new ChatRoomMessage(
            3,
            Guid.NewGuid(),
            ChatRoomMessageKind.Assistant,
            "自身回答",
            DateTimeOffset.UtcNow,
            "assistant",
            "助手");
        var system = new ChatRoomMessage(
            4,
            Guid.NewGuid(),
            ChatRoomMessageKind.System,
            "系统消息",
            DateTimeOffset.UtcNow);

        IReadOnlyList<string> singleAi = IsolatedChatRoomRoleRuntime.BuildIncrementalMessages(
            [human, assistant, own, system],
            "assistant",
            omitHumanSenderPrefix: true);
        IReadOnlyList<string> multipleAi = IsolatedChatRoomRoleRuntime.BuildIncrementalMessages(
            [human, assistant, own, system],
            "assistant",
            omitHumanSenderPrefix: false);

        CollectionAssert.AreEqual(new[] { "问题", "其他助手说：历史回答" }, singleAi.ToArray());
        CollectionAssert.AreEqual(new[] { "用户说：问题", "其他助手说：历史回答" }, multipleAi.ToArray());
    }

    [TestMethod(DisplayName = "替换运行时后旧租约应延迟退休旧资源")]
    [Timeout(5000)]
    public async Task ReplaceShouldRetireOldRuntimeAfterLeaseRelease()
    {
        var created = new List<TrackingRuntime>();
        var registry = new ChatRoomRoleRuntimeRegistry(
        [
            new TestRuntimeFactory(ChatRoomRoleExecutionKind.Standard, definition =>
            {
                var runtime = new TrackingRuntime(definition);
                created.Add(runtime);
                return runtime;
            }),
        ]);
        ChatRoomRoleDefinition firstDefinition = CreateDefinition(
            "assistant",
            ChatRoomRoleExecutionKind.Standard,
            runtimeVersion: 1);
        ChatRoomRoleDefinition secondDefinition = CreateDefinition(
            "assistant",
            ChatRoomRoleExecutionKind.Standard,
            runtimeVersion: 2);
        await registry.AddAsync(firstDefinition);
        ChatRoomRoleRuntimeLease firstLease = registry.Acquire(firstDefinition);

        await registry.ReplaceAsync(secondDefinition);

        Assert.AreEqual(0, created[0].DisposeCount);
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => registry.Acquire(firstDefinition));
        await using (ChatRoomRoleRuntimeLease secondLease = registry.Acquire(secondDefinition))
        {
            Assert.AreEqual(2L, secondLease.RuntimeVersion);
        }
        await firstLease.DisposeAsync();
        await created[0].Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, created[0].DisposeCount);

        await registry.DisposeAsync();
        Assert.AreEqual(1, created[1].DisposeCount);
    }

    [TestMethod(DisplayName = "注册表关闭应等待活动运行时租约")]
    [Timeout(5000)]
    public async Task DisposeShouldWaitForActiveRuntimeLease()
    {
        TrackingRuntime? runtime = null;
        var registry = new ChatRoomRoleRuntimeRegistry(
        [
            new TestRuntimeFactory(ChatRoomRoleExecutionKind.Standard, definition =>
                runtime = new TrackingRuntime(definition)),
        ]);
        ChatRoomRoleDefinition definition = CreateDefinition(
            "assistant",
            ChatRoomRoleExecutionKind.Standard,
            runtimeVersion: 1);
        await registry.AddAsync(definition);
        ChatRoomRoleRuntimeLease lease = registry.Acquire(definition);

        Task disposeTask = registry.DisposeAsync().AsTask();

        Assert.IsFalse(disposeTask.IsCompleted);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => registry.Acquire(definition));
        await lease.DisposeAsync();
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, runtime!.DisposeCount);
    }

    [TestMethod(DisplayName = "工厂返回事实不一致的运行时时应释放后拒绝注册")]
    [Timeout(5000)]
    public async Task RegistryShouldDisposeMismatchedRuntimeFromFactory()
    {
        TrackingRuntime? mismatched = null;
        var registry = new ChatRoomRoleRuntimeRegistry(
        [
            new TestRuntimeFactory(ChatRoomRoleExecutionKind.Standard, definition =>
            {
                ChatRoomRoleDefinition wrongDefinition = CreateDefinition(
                    definition.Identity.RoleId,
                    definition.ExecutionKind,
                    runtimeVersion: definition.RuntimeVersion + 1);
                return mismatched = new TrackingRuntime(wrongDefinition);
            }),
        ]);
        ChatRoomRoleDefinition definition = CreateDefinition(
            "assistant",
            ChatRoomRoleExecutionKind.Standard,
            runtimeVersion: 1);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => registry.AddAsync(definition));

        Assert.AreEqual(1, mismatched!.DisposeCount);
        await registry.DisposeAsync();
    }

    private static ChatRoomRoleDefinition CreateDefinition(
        string roleId,
        ChatRoomRoleExecutionKind executionKind,
        long runtimeVersion) => new(
        new ChatRoomRoleIdentity(roleId, incarnation: 0),
        executionKind,
        roleId,
        "系统提示词",
        isHuman: false,
        runtimeVersion: runtimeVersion);

    private static ChatRoomMessage CreateHumanMessage() => new(
        messageSequence: 1,
        Guid.NewGuid(),
        ChatRoomMessageKind.Human,
        "问题",
        DateTimeOffset.UtcNow,
        "human",
        "用户");

    private sealed class TestRuntimeFactory(
        ChatRoomRoleExecutionKind executionKind,
        Func<ChatRoomRoleDefinition, IChatRoomRoleRuntime> createRuntime)
        : IChatRoomRoleRuntimeFactory
    {
        public ChatRoomRoleExecutionKind ExecutionKind { get; } = executionKind;

        public Task<IChatRoomRoleRuntime> CreateAsync(
            ChatRoomRoleDefinition definition,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(createRuntime(definition));
        }
    }

    private sealed class TrackingRuntime(ChatRoomRoleDefinition definition) : IChatRoomRoleRuntime
    {
        public int DisposeCount { get; private set; }

        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ChatRoomRoleIdentity Identity { get; } = definition.Identity;

        public ChatRoomRoleExecutionKind ExecutionKind { get; } = definition.ExecutionKind;

        public long RuntimeVersion { get; } = definition.RuntimeVersion;

        public Task<ChatRoomRoleExecutionCandidate> ExecuteAsync(
            ChatRoomRoleExecutionRequest request,
            IChatRoomRoleExecutionEventSink? eventSink,
            CancellationToken cancellationToken)
        {
            _ = eventSink;
            cancellationToken.ThrowIfCancellationRequested();
            var checkpoint = new ChatRoomRoleCheckpointCandidate(
                Identity,
                RuntimeVersion,
                ExecutionKind,
                sessionRevision: 1,
                consumedThroughSequence: request.InputThroughSequence,
                serializerVersion: 1,
                ChatRoomRoleCheckpointFormats.AgentSessionJsonV1,
                new byte[] { 1 });
            return Task.FromResult(new ChatRoomRoleExecutionCandidate(
                request.ExecutionId,
                Identity,
                "完成",
                "Fake",
                checkpoint));
        }

        public Task<IChatRoomRoleWorkspaceTransaction> PrepareWorkspaceChangeAsync(
            string? workspacePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IChatRoomRoleWorkspaceTransaction>(new TestWorkspaceTransaction(workspacePath));
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            Disposed.TrySetResult();
            return default;
        }
    }

    private sealed class TestWorkspaceTransaction(string? workspacePath) : IChatRoomRoleWorkspaceTransaction
    {
        public string? WorkspacePath { get; } = workspacePath;

        public void Apply()
        {
        }

        public ValueTask RollbackAsync() => default;

        public void CommitAfterPublish()
        {
        }

        public ValueTask DisposeAsync() => default;
    }
}
