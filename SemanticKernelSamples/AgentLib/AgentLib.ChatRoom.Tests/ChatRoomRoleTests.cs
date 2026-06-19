using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.Core;

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
    public void SpeakAsync_WithNullInput_ThrowsArgumentNullException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        Assert.ThrowsExactly<ArgumentNullException>(
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
    public void SpeakAsync_WithEmptyStringInput_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        Assert.ThrowsExactly<ArgumentException>(
            () => role.SpeakAsync(string.Empty));
    }

    [TestMethod]
    public void SpeakAsync_WithWhitespaceInput_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        Assert.ThrowsExactly<ArgumentException>(
            () => role.SpeakAsync("   "));
    }

    [TestMethod]
    public void SpeakFirstAsync_WithNullInput_ThrowsArgumentNullException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        Assert.ThrowsExactly<ArgumentNullException>(
            () => role.SpeakFirstAsync(null!));
    }

    [TestMethod]
    public void SpeakFirstAsync_WithEmptyStringInput_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        Assert.ThrowsExactly<ArgumentException>(
            () => role.SpeakFirstAsync(string.Empty));
    }

    [TestMethod]
    public void SpeakFirstAsync_WithWhitespaceInput_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);

        Assert.ThrowsExactly<ArgumentException>(
            () => role.SpeakFirstAsync("   "));
    }
}