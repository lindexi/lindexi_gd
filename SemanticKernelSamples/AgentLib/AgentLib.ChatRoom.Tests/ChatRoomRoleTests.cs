using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Moq;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomRoleTests
{
    [TestMethod]
    public void Constructor_WithNullDefinition_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ChatRoomRole(null!));
    }

    [TestMethod]
    public void Constructor_WithValidDefinition_SetsDefinitionProperty()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };

        var role = new ChatRoomRole(definition);

        Assert.AreSame(definition, role.Definition);
    }

    [TestMethod]
    public void Constructor_WithNullEndpointManager_CreatesDefaultEndpointManager()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };

        var role = new ChatRoomRole(definition, endpointManager: null);

        Assert.IsNotNull(role.EndpointManager);
    }

    [TestMethod]
    public void Constructor_WithProvidedEndpointManager_UsesProvidedEndpointManager()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var endpointManager = new AgentApiEndpointManager();

        var role = new ChatRoomRole(definition, endpointManager);

        Assert.AreSame(endpointManager, role.EndpointManager);
    }

    [TestMethod]
    public void EndpointManager_ReturnsSameInstanceAsProvidedToConstructor()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
        };
        var endpointManager = new AgentApiEndpointManager();
        var role = new ChatRoomRole(definition, endpointManager);

        var result = role.EndpointManager;

        Assert.AreSame(endpointManager, result);
    }

    [TestMethod]
    public void EndpointManager_DefaultInstance_IsNotNull()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
        };
        var role = new ChatRoomRole(definition);

        var result = role.EndpointManager;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<AgentApiEndpointManager>(result);
    }

    [TestMethod]
    public void MainThreadDispatcher_DefaultValue_IsNull()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
        };
        var role = new ChatRoomRole(definition);

        Assert.IsNull(role.MainThreadDispatcher);
    }

    [TestMethod]
    public void MainThreadDispatcher_CanBeSetViaObjectInitializer()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
        };
        var mockDispatcher = new Mock<IMainThreadDispatcher>();

        var role = new ChatRoomRole(definition)
        {
            MainThreadDispatcher = mockDispatcher.Object,
        };

        Assert.AreSame(mockDispatcher.Object, role.MainThreadDispatcher);
    }

    [TestMethod]
    public async Task InitializeAsync_WithEmptySkillFolders_CompletesSuccessfully()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        await role.InitializeAsync();
    }

    [TestMethod]
    public async Task InitializeAsync_WithNonExistentSkillFolder_DoesNotThrow()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            SkillFolders = { "C:\\NonExistent\\SkillFolder\\Path" },
        };
        var role = new ChatRoomRole(definition);

        await role.InitializeAsync();
    }

    [TestMethod]
    public async Task SpeakAsync_WithNullInput_ThrowsArgumentNullException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => role.SpeakAsync(null!));
    }

    [TestMethod]
    public async Task InitializeAsync_WithModelProviderIdAndModelIdSet_CompletesWithoutThrowing()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            ModelProviderId = "test-provider",
            ModelId = "test-model",
        };
        var role = new ChatRoomRole(definition);

        await role.InitializeAsync();
    }

    [TestMethod]
    public async Task SpeakAsync_WithEmptyMessages_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => role.SpeakAsync(Array.Empty<string>()));
    }

    [TestMethod]
    public async Task SpeakAsync_WithWhitespaceInput_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => role.SpeakAsync(new[] { "   " }));
    }

    [TestMethod]
    public async Task SpeakFirstAsync_WithNullInput_ThrowsArgumentNullException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => role.SpeakFirstAsync(null!));
    }

    [TestMethod]
    public async Task SpeakFirstAsync_WithEmptyStringInput_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => role.SpeakFirstAsync(string.Empty));
    }

    [TestMethod]
    public async Task SpeakFirstAsync_WithWhitespaceInput_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => role.SpeakFirstAsync("   "));
    }

    [TestMethod]
    public void EnsureModelAvailable_WithNoModels_ThrowsInvalidOperationException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        Assert.ThrowsExactly<InvalidOperationException>(() => role.EnsureModelAvailable());
    }

    [TestMethod]
    public void EnsureModelAvailable_WithModels_DoesNotThrow()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        var mockProvider = new Mock<ILanguageModelProvider>();
        var mockModel = new Mock<ILanguageModel>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModel.Object });

        role.EndpointManager.RegisterLanguageModelProvider(mockProvider.Object);

        role.EnsureModelAvailable();
    }

    [TestMethod]
    public void EnsureModelAvailable_ExceptionMessage_ContainsRoleName()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "我的测试角色",
        };
        var role = new ChatRoomRole(definition);

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => role.EnsureModelAvailable());

        Assert.Contains("我的测试角色", ex.Message);
    }
}