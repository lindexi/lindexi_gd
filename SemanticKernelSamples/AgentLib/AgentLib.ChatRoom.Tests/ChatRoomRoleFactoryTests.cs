using AgentLib.ChatRoom.Model;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Moq;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomRoleFactoryTests
{
    [TestMethod(DisplayName = "角色工厂应保留角色定义")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRoleShouldKeepDefinition()
    {
        var factory = new ChatRoomRoleFactory();
        ChatRoomRoleDefinition definition = CreateDefinition();

        ChatRoomRole role = factory.CreateRole(definition);

        Assert.AreSame(definition, role.Definition);
    }

    [TestMethod(DisplayName = "角色工厂应设置主线程调度器")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateRoleShouldSetMainThreadDispatcher()
    {
        var dispatcher = new Mock<IMainThreadDispatcher>();
        var factory = new ChatRoomRoleFactory(dispatcher.Object);

        ChatRoomRole role = factory.CreateRole(CreateDefinition());

        Assert.AreSame(dispatcher.Object, role.MainThreadDispatcher);
    }

    [TestMethod(DisplayName = "Standard 定义应创建 Standard 执行器")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateStandardRoleShouldUseStandardExecutor()
    {
        var factory = new ChatRoomRoleFactory();

        ChatRoomRole role = factory.CreateRole(CreateDefinition());

        Assert.AreEqual(ChatRoomRoleExecutionKind.Standard, role.Executor.ExecutionKind);
        Assert.IsInstanceOfType<StandardChatRoomRoleExecutor>(role.Executor);
    }

    [TestMethod(DisplayName = "Coding 定义应创建 Coding 执行器")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateCodingRoleShouldUseCodingExecutor()
    {
        var factory = new ChatRoomRoleFactory();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            RoleName = "编程角色",
            IsHuman = false,
        };

        ChatRoomRole role = factory.CreateRole(definition);

        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, role.Executor.ExecutionKind);
        Assert.IsInstanceOfType<CodingChatRoomRoleExecutor>(role.Executor);
    }

    [TestMethod(DisplayName = "人类角色不能使用 Coding 执行种类")]
    public void CreateRoleShouldRejectHumanCodingDefinition()
    {
        var factory = new ChatRoomRoleFactory();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-human",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            IsHuman = true,
        };

        Assert.ThrowsExactly<ArgumentException>(() => factory.CreateRole(definition));
    }

    [TestMethod(DisplayName = "未知执行种类应被拒绝")]
    public void CreateRoleShouldRejectUnknownExecutionKind()
    {
        var factory = new ChatRoomRoleFactory();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "unknown-role",
            ExecutionKind = (ChatRoomRoleExecutionKind)99,
        };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => factory.CreateRole(definition));
    }

    [TestMethod(DisplayName = "重复注册同一执行种类应被拒绝")]
    public void ConstructorShouldRejectDuplicateExecutionKindFactories()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomRoleFactory(
            null,
            [
                new RecordingExecutorFactory(ChatRoomRoleExecutionKind.Standard),
                new RecordingExecutorFactory(ChatRoomRoleExecutionKind.Standard),
            ]));
    }

    [TestMethod(DisplayName = "缺少执行器工厂时应立即失败")]
    public void CreateRoleShouldFailWhenFactoryRegistrationIsMissing()
    {
        var factory = new ChatRoomRoleFactory(
            null,
            [new RecordingExecutorFactory(ChatRoomRoleExecutionKind.Standard)]);
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
        };

        Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateRole(definition));
    }

    [TestMethod(DisplayName = "工厂返回错误执行种类时应立即失败并释放执行器")]
    public void CreateRoleShouldFailWhenFactoryReturnsWrongExecutorKind()
    {
        var executor = new RecordingExecutor(ChatRoomRoleExecutionKind.Coding);
        var factory = new ChatRoomRoleFactory(
            null,
            [new RecordingExecutorFactory(ChatRoomRoleExecutionKind.Standard, executor)]);

        Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateRole(CreateDefinition()));
        Assert.AreEqual(1, executor.DisposeCount);
    }

    [TestMethod(DisplayName = "管理器按定义添加角色时应使用角色工厂")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task AddRoleDefinitionShouldUseRoleFactory()
    {
        var roleFactory = new RecordingRoleFactory();
        var manager = new ChatRoomManager(new ChatRoomSession(), roleFactory);
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        ChatRoomRoleDefinition definition = CreateDefinition();

        ChatRoomRole role = await manager.AddRoleAsync(definition);

        Assert.AreSame(definition, roleFactory.LastDefinition);
        Assert.AreSame(roleFactory.LastRole, role);
    }

    [TestMethod(DisplayName = "历史加载角色时应使用角色工厂")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task LoadAsyncShouldUseRoleFactory()
    {
        string persistencePath = Path.Join(Path.GetTempPath(), "ChatRoomRoleFactoryTests", Path.GetRandomFileName());
        var persistence = new ChatRoomPersistence(persistencePath);
        var sessionData = new ChatRoomSessionData
        {
            SessionId = Guid.NewGuid(),
            Title = "测试会话",
            CreatedAt = DateTimeOffset.Now,
            LastActivityAt = DateTimeOffset.Now,
            Roles = [CreateDefinition()],
        };
        await persistence.SaveConfigAsync(sessionData);
        var roleFactory = new RecordingRoleFactory();
        var manager = new ChatRoomManager(new ChatRoomSession(), roleFactory)
        {
            Persistence = persistence,
        };
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

        await manager.LoadAsync(sessionData.SessionId.ToString("N"));

        Assert.AreEqual(sessionData.Roles.Single().RoleId, roleFactory.LastDefinition?.RoleId);
        Assert.AreSame(roleFactory.LastRole, manager.Roles.Single());
    }

    private static ChatRoomRoleDefinition CreateDefinition() => new()
    {
        RoleId = "role-1",
        RoleName = "测试角色",
        IsHuman = true,
    };

    private sealed class RecordingRoleFactory : IChatRoomRoleFactory
    {
        public ChatRoomRoleDefinition? LastDefinition { get; private set; }

        public ChatRoomRole? LastRole { get; private set; }

        public ChatRoomRole CreateRole(ChatRoomRoleDefinition definition)
        {
            LastDefinition = definition;
            LastRole = new ChatRoomRole(definition);
            return LastRole;
        }
    }

    private sealed class RecordingExecutorFactory(
        ChatRoomRoleExecutionKind executionKind,
        IChatRoomRoleExecutor? executor = null) : IChatRoomRoleExecutorFactory
    {
        public ChatRoomRoleExecutionKind ExecutionKind { get; } = executionKind;

        public IChatRoomRoleExecutor Create(ChatRoomRoleExecutorCreationContext context)
        {
            return executor ?? new RecordingExecutor(ExecutionKind);
        }
    }

    private sealed class RecordingExecutor(ChatRoomRoleExecutionKind executionKind) : IChatRoomRoleExecutor
    {
        public ChatRoomRoleExecutionKind ExecutionKind { get; } = executionKind;

        public int DisposeCount { get; private set; }

        public Task<ChatRoomRoleExecutionResult> RunAsync(
            ChatRoomRoleExecutionContext context,
            IReadOnlyList<Microsoft.Extensions.AI.AIContent> contents,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetWorkspacePathAsync(
            CopilotChatManager chatManager,
            string? workspacePath,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return default;
        }
    }
}
