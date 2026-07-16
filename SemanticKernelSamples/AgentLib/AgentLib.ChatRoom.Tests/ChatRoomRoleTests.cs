using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using Moq;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomRoleTests
{
    [TestMethod(DisplayName = "合并工具时本轮工具应覆盖同名运行时工具")]
    public void MergeToolsShouldPreferAdditionalTool()
    {
        AITool defaultTool = CreateTool("same_tool");
        AITool runtimeTool = CreateTool("same_tool");
        AITool additionalTool = CreateTool("same_tool");

        IReadOnlyList<AITool> result = ChatRoomRole.MergeTools([defaultTool], [runtimeTool], [additionalTool]);

        Assert.AreSame(additionalTool, result.Single());
    }

    [TestMethod(DisplayName = "合并工具时运行时工具应覆盖同名默认工具")]
    public void MergeToolsShouldPreferRuntimeTool()
    {
        AITool defaultTool = CreateTool("same_tool");
        AITool runtimeTool = CreateTool("same_tool");

        IReadOnlyList<AITool> result = ChatRoomRole.MergeTools([defaultTool], [runtimeTool], null);

        Assert.AreSame(runtimeTool, result.Single());
    }

    [TestMethod(DisplayName = "合并工具时应按名称去重并保持优先级顺序")]
    public void MergeToolsShouldDeduplicateByOrdinalNameInPriorityOrder()
    {
        AITool additionalTool = CreateTool("shared_tool");
        AITool runtimeTool = CreateTool("runtime_tool");
        AITool defaultTool = CreateTool("default_tool");

        IReadOnlyList<AITool> result = ChatRoomRole.MergeTools(
            [defaultTool, CreateTool("shared_tool")],
            [runtimeTool, CreateTool("shared_tool")],
            [additionalTool]);

        CollectionAssert.AreEqual(
            new[] { "shared_tool", "runtime_tool", "default_tool" },
            result.Select(tool => tool.Name).ToArray());
    }

    [TestMethod(DisplayName = "设置工作区应发布路径和工具快照")]
    public async Task SetWorkspaceShouldPublishPathAndToolSnapshot()
    {
        string workspacePath = CreateWorkspacePath();
        var roleTool = new TestChatRoomRoleTool(CreateTool("workspace_tool"));
        await using ChatRoomRole role = CreateRole(roleTool);

        await role.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        Assert.AreEqual(workspacePath, role.WorkspacePath);
        Assert.AreSame(roleTool.AITools.Single(), role.WorkspaceTools.Single());
    }

    [TestMethod(DisplayName = "设置工作区不应通知普通角色工具")]
    public async Task SetWorkspaceShouldNotNotifyRegularRoleTool()
    {
        string workspacePath = CreateWorkspacePath();
        var roleTool = new RegularChatRoomRoleTool(CreateTool("regular_tool"));
        await using ChatRoomRole role = CreateRole(roleTool);

        await role.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        Assert.AreEqual(workspacePath, role.WorkspacePath);
        Assert.AreEqual(0, roleTool.WorkspaceNotificationCount);
    }

    [TestMethod(DisplayName = "切换工作区应替换工具快照")]
    public async Task SetWorkspaceShouldReplaceToolSnapshot()
    {
        string firstPath = CreateWorkspacePath();
        string secondPath = CreateWorkspacePath();
        var roleTool = new TestChatRoomRoleTool(CreateTool("workspace_tool"));
        await using ChatRoomRole role = CreateRole(roleTool);
        await role.SetWorkspacePathAsync(firstPath, CancellationToken.None);

        await role.SetWorkspacePathAsync(secondPath, CancellationToken.None);

        Assert.AreEqual(secondPath, role.WorkspacePath);
        Assert.AreEqual(secondPath, roleTool.WorkspacePath);
    }

    [TestMethod(DisplayName = "角色发言期间设置工作区不应等待发言完成")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task SetWorkspaceDuringSpeakingShouldNotWaitForSpeakingToComplete()
    {
        string workspacePath = CreateWorkspacePath();
        var roleTool = new TestChatRoomRoleTool(CreateTool("workspace_tool"));
        var streamStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseStream = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeChatClient client = CreateBlockingClient(streamStarted, releaseStream);
        ChatRoomRole role = CreateRole(roleTool, "test-provider");
        ChatRoomSpeakResult? result = null;
        try
        {
            RegisterFakeModel(role, "test-provider", client);
            result = await role.SpeakAsync(["开始"]);
            Assert.IsNotNull(result);
            await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await role.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

            Assert.IsFalse(result.FinalContentTask.IsCompleted);
            Assert.AreSame(roleTool.AITools.Single(), role.WorkspaceTools.Single());
        }
        finally
        {
            releaseStream.TrySetResult();
            if (result is not null)
            {
                await result.FinalContentTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            await role.DisposeAsync();
        }
    }

    [TestMethod(DisplayName = "异步释放重复调用应保持幂等")]
    public async Task DisposeAsyncShouldBeIdempotent()
    {
        var roleTool = new TestChatRoomRoleTool(CreateTool("workspace_tool"));
        ChatRoomRole role = CreateRole(roleTool);
        await role.SetWorkspacePathAsync(CreateWorkspacePath(), CancellationToken.None);

        await role.DisposeAsync();
        await role.DisposeAsync();

        Assert.AreEqual(1, roleTool.DisposeCount);
    }

    [TestMethod]
    public void Constructor_WithNullDefinition_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ChatRoomRole(null!));
    }

    private static AITool CreateTool(string name) => AIFunctionFactory.Create(
        () => string.Empty,
        name);

    private static ChatRoomRole CreateRole() => new(CreateDefinition());

    private static ChatRoomRole CreateRole(
        IChatRoomRoleTool roleTool,
        string? providerId = null) =>
        new(CreateDefinition(providerId), null, [roleTool]);

    private static ChatRoomRoleDefinition CreateDefinition(string? providerId = null) => new()
    {
        RoleId = "role-1",
        RoleName = "测试角色",
        ModelProviderId = providerId,
    };

    private static string CreateWorkspacePath()
    {
        string workspacePath = Path.Join(Path.GetTempPath(), "ChatRoomRoleTests", Path.GetRandomFileName());
        Directory.CreateDirectory(workspacePath);
        return workspacePath;
    }

    private static void RegisterFakeModel(ChatRoomRole role, string providerName, FakeChatClient client)
    {
        var model = new FakeLanguageModel(client)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = providerName,
                ModelName = "Fake",
                ModelId = "Fake",
            },
        };
        role.EndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([model]));
    }

    private sealed class TestChatRoomRoleTool(AITool aiTool) : IChatRoomRoleTool, IChatRoomWorkspaceAwareTool
    {
        public IReadOnlyList<AITool> AITools { get; } = [aiTool];

        public string? WorkspacePath { get; private set; }

        public int DisposeCount { get; private set; }

        public Task SetWorkspacePathAsync(string? workspacePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorkspacePath = workspacePath;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RegularChatRoomRoleTool(AITool aiTool) : IChatRoomRoleTool
    {
        public IReadOnlyList<AITool> AITools { get; } = [aiTool];

        public int WorkspaceNotificationCount { get; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static FakeChatClient CreateBlockingClient(
        TaskCompletionSource streamStarted,
        TaskCompletionSource releaseStream)
    {
        var client = new FakeChatClient();
        client.OnGetStreamingResponseAsync = (_, _, cancellationToken) => StreamBlockingResponseAsync(
            streamStarted,
            releaseStream,
            cancellationToken);
        client.OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "完成")));
        return client;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamBlockingResponseAsync(
        TaskCompletionSource streamStarted,
        TaskCompletionSource releaseStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        streamStarted.SetResult();
        await releaseStream.Task.WaitAsync(cancellationToken);
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("完成")],
        };
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
        string missingSkillFolder = Path.Join(Path.GetTempPath(), "ChatRoomRoleTests", Path.GetRandomFileName());
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            SkillFolders = { missingSkillFolder },
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