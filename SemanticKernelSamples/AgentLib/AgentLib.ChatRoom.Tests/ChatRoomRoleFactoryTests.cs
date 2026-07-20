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

    [TestMethod(DisplayName = "普通角色不应获得编程运行时工具")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateStandardRoleShouldNotAttachCodingTools()
    {
        var factory = new ChatRoomRoleFactory();

        ChatRoomRole role = factory.CreateRole(CreateDefinition());

        Assert.IsEmpty(role.RoleTools);
    }

    [TestMethod(DisplayName = "编程助手角色应按当前代码获得编程运行时工具")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void CreateCodingAssistantRoleShouldAttachCodingTools()
    {
        var factory = new ChatRoomRoleFactory();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            Kind = ChatRoomRoleKind.CodingAssistant,
            RoleName = "编程角色",
            IsHuman = true,
        };

        ChatRoomRole role = factory.CreateRole(definition);

        Assert.IsNotEmpty(role.RoleTools);
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
}
