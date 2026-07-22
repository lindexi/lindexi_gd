using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.Coding;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using Moq;

using System.Runtime.CompilerServices;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomRoleTests
{
    [TestMethod(DisplayName = "便捷构造函数只允许 Standard 执行种类")]
    public void Constructor_WithCodingDefinition_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
        };

        Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomRole(definition));
    }

    [TestMethod(DisplayName = "定义与执行器种类不一致时应立即失败")]
    public void Constructor_WithMismatchedExecutor_ThrowsArgumentException()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            ExecutionKind = ChatRoomRoleExecutionKind.Standard,
        };
        var executor = new RecordingExecutor(ChatRoomRoleExecutionKind.Coding);

        Assert.ThrowsExactly<ArgumentException>(() => new ChatRoomRole(definition, null, executor));
    }

    [TestMethod(DisplayName = "SpeakAsync 应只委托统一执行器并复用同一流式消息")]
    public async Task SpeakAsync_ShouldDelegateToExecutorAndReuseMessage()
    {
        var executor = new RecordingExecutor(ChatRoomRoleExecutionKind.Standard);
        await using var role = new ChatRoomRole(CreateDefinition("test-provider"), null, executor);
        RegisterFakeModel(role, "test-provider");
        AITool additionalTool = AIFunctionFactory.Create(() => string.Empty, "additional_tool");

        ChatRoomSpeakResult? result = await role.SpeakAsync(["第一条", "第二条"], [additionalTool]);

        Assert.IsNotNull(result);
        Assert.AreSame(executor.AssistantMessage, result.AssistantChatMessage);
        CollectionAssert.AreEqual(
            new[] { "第一条", "第二条" },
            executor.Contents!.OfType<TextContent>().Select(content => content.Text).ToArray());
        Assert.AreSame(additionalTool, executor.Context!.AdditionalTools.Single());
        StringAssert.Contains(executor.Context.SystemPrompt, "测试角色");

        executor.Completion.TrySetResult(new ChatRoomRoleExecutionCompletion("完成", WasCanceled: false));
        Assert.AreEqual("完成", await result.FinalContentTask);
    }

    [TestMethod(DisplayName = "角色发言期间设置工作区不应等待发言完成")]
    public async Task SetWorkspaceDuringSpeakingShouldNotWaitForSpeakingToComplete()
    {
        var executor = new RecordingExecutor(ChatRoomRoleExecutionKind.Standard);
        await using var role = new ChatRoomRole(CreateDefinition("test-provider"), null, executor);
        RegisterFakeModel(role, "test-provider");
        ChatRoomSpeakResult? result = await role.SpeakAsync(["开始"]);
        Assert.IsNotNull(result);
        string workspacePath = CreateWorkspacePath();

        await role.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        Assert.IsFalse(result.FinalContentTask.IsCompleted);
        Assert.AreEqual(workspacePath, executor.WorkspacePath);
        executor.Completion.TrySetResult(new ChatRoomRoleExecutionCompletion("完成", WasCanceled: false));
        await result.FinalContentTask;
    }

    [TestMethod(DisplayName = "SpeakAsync 不应把执行器内部取消误判为调用方取消")]
    [Timeout(5000)]
    public async Task SpeakAsyncShouldPropagateUnrelatedOperationCanceledException()
    {
        await using var role = new ChatRoomRole(
            CreateDefinition("test-provider"),
            null,
            new ThrowingExecutor(new OperationCanceledException("内部超时")));
        RegisterFakeModel(role, "test-provider");

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => role.SpeakAsync(["开始"]));
    }

    [TestMethod(DisplayName = "Coding 角色编辑和工作区切换不应替换 AgentSession")]
    [Timeout(15000)]
    public async Task CodingRoleEditAndWorkspaceChangeShouldPreserveAgentSession()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) => ImmediateStreamAsync(cancellationToken),
        };
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            RoleName = "编程角色",
            ModelProviderId = "test-provider",
        };
        var model = new FakeLanguageModel(client)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = "test-provider",
                ModelName = "Fake",
                ModelId = "Fake",
            },
        };
        var provider = new FakeLanguageModelProvider([model]);
        await using var role = new ChatRoomRole(
            definition,
            null,
            new CodingChatRoomRoleExecutor(new CodingAgent($"missing-roslyn-{Guid.NewGuid():N}")));
        await using var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = provider,
        });
        await manager.AddRoleAsync(role);
        ChatRoomSpeakResult firstRun = (await role.SpeakAsync(["初始任务"]))!;
        await firstRun.FinalContentTask;
        string stateBeforeChanges = (await role.SerializeAgentSessionStateAsync())!.Value.GetRawText();

        await manager.SetWorkspacePathAsync(CreateWorkspacePath());
        string stateAfterWorkspaceChange = (await role.SerializeAgentSessionStateAsync())!.Value.GetRawText();
        await manager.UpdateRoleAsync(
            definition.RoleId,
            "高级编程角色",
            definition.SystemPrompt,
            isHuman: false,
            definition.ModelProviderId,
            definition.ModelId,
            definition.MemoryContent,
            definition.ParticipationMode);
        string stateAfterEdit = (await role.SerializeAgentSessionStateAsync())!.Value.GetRawText();

        Assert.AreEqual(stateBeforeChanges, stateAfterWorkspaceChange);
        Assert.AreEqual(stateBeforeChanges, stateAfterEdit);
        StringAssert.Contains(stateAfterEdit, "初始任务");
        StringAssert.Contains(stateAfterEdit, "完成");
        Assert.AreSame(role, manager.Roles.Single());
        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, role.Definition.ExecutionKind);
    }

    [TestMethod(DisplayName = "异步释放重复调用应只释放统一执行器一次")]
    public async Task DisposeAsyncShouldBeIdempotent()
    {
        var executor = new RecordingExecutor(ChatRoomRoleExecutionKind.Standard);
        var role = new ChatRoomRole(CreateDefinition(), null, executor);

        await role.DisposeAsync();
        await role.DisposeAsync();

        Assert.AreEqual(1, executor.DisposeCount);
    }

    [TestMethod]
    public void Constructor_WithNullDefinition_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ChatRoomRole(null!));
    }

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

    private static void RegisterFakeModel(
        ChatRoomRole role,
        string providerName,
        FakeChatClient? client = null)
    {
        var model = new FakeLanguageModel(client ?? new FakeChatClient())
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

    private static async IAsyncEnumerable<ChatResponseUpdate> ImmediateStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("完成")]);
        await Task.CompletedTask;
    }

    private sealed class RecordingExecutor(ChatRoomRoleExecutionKind executionKind) : IChatRoomRoleExecutor
    {
        public ChatRoomRoleExecutionKind ExecutionKind { get; } = executionKind;

        public CopilotChatMessage AssistantMessage { get; } = CopilotChatMessage.CreateAssistant("...", isPresetInfo: false);

        public TaskCompletionSource<ChatRoomRoleExecutionCompletion> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ChatRoomRoleExecutionContext? Context { get; private set; }

        public IReadOnlyList<AIContent>? Contents { get; private set; }

        public string? WorkspacePath { get; private set; }

        public int DisposeCount { get; private set; }

        public Task<ChatRoomRoleExecutionResult> RunAsync(
            ChatRoomRoleExecutionContext context,
            IReadOnlyList<AIContent> contents,
            CancellationToken cancellationToken)
        {
            Context = context;
            Contents = [.. contents];
            return Task.FromResult(new ChatRoomRoleExecutionResult(AssistantMessage, Completion.Task));
        }

        public Task SetWorkspacePathAsync(
            CopilotChatManager chatManager,
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorkspacePath = workspacePath;
            chatManager.WorkspacePath = workspacePath;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return default;
        }
    }

    private sealed class ThrowingExecutor(Exception exception) : IChatRoomRoleExecutor
    {
        public ChatRoomRoleExecutionKind ExecutionKind => ChatRoomRoleExecutionKind.Standard;

        public Task<ChatRoomRoleExecutionResult> RunAsync(
            ChatRoomRoleExecutionContext context,
            IReadOnlyList<AIContent> contents,
            CancellationToken cancellationToken) => Task.FromException<ChatRoomRoleExecutionResult>(exception);

        public Task SetWorkspacePathAsync(
            CopilotChatManager chatManager,
            string? workspacePath,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => default;
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