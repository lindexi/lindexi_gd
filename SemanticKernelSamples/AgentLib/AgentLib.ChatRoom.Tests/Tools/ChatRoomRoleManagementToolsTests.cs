using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Microsoft.Extensions.AI;

using Moq;

namespace AgentLib.ChatRoom.Tests.Tools;

/// <summary>
/// <see cref="ChatRoomRoleManagementTools"/> 的单元测试。
/// 重点验证 create_character 工具创建的新角色是否被正确注入模型。
/// </summary>
[TestClass]
public sealed class ChatRoomRoleManagementToolsTests
{
    /// <summary>
    /// 创建一个返回单个 mock 模型的 provider。
    /// </summary>
    private static Mock<ILanguageModelProvider> CreateMockProvider(string providerName)
    {
        var mockModel = new Mock<ILanguageModel>();
        mockModel.SetupGet(m => m.ModelDefinition)
            .Returns(new ModelDefinition
            {
                Provider = providerName,
                ModelName = "test-model",
                ModelId = "test-model-id",
            });

        var mockProvider = new Mock<ILanguageModelProvider>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModel.Object });

        return mockProvider;
    }

    /// <summary>
    /// 从工具集合中按名称查找指定工具。
    /// </summary>
    private static AIFunction GetTool(IReadOnlyList<AITool> tools, string name)
    {
        AIFunction? tool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Name == name);
        Assert.IsNotNull(tool, $"未找到名为 {name} 的工具");
        return tool;
    }

    private static string GetResultString(object? resultObj)
    {
        Assert.IsNotNull(resultObj);
        return resultObj.ToString() ?? string.Empty;
    }

    [TestMethod(DisplayName = "create_character 使用已注册提供商时新角色应获得模型")]
    public async Task CreateCharacter_WithRegisteredProviders_NewRoleHasModels()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("test-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object? resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "代码审查员",
                ["systemPrompt"] = "你是一位资深代码审查员",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("✅", result);
        Assert.HasCount(1, manager.Roles);

        ChatRoomRole newRole = manager.Roles[0];
        Assert.HasCount(1, newRole.EndpointManager.GetSupportedModels());
    }

    [TestMethod(DisplayName = "create_character 输入有效时应添加角色")]
    public async Task CreateCharacter_ValidInputs_RoleAddedToRoles()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("test-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object? resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "架构师",
                ["systemPrompt"] = "你是一位架构师",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = "熟悉微服务架构",
            });

        string result = GetResultString(resultObj);
        Assert.Contains("✅", result);
        Assert.Contains("架构师", result);
        Assert.HasCount(1, manager.Roles);
        Assert.AreEqual("架构师", manager.Roles[0].Definition.RoleName);
        Assert.IsFalse(manager.Roles[0].Definition.IsHuman);
        Assert.AreEqual(ChatRoomParticipationMode.MentionOnly, manager.Roles[0].Definition.ParticipationMode);
        Assert.AreEqual(ChatRoomRoleExecutionKind.Standard, manager.Roles[0].Definition.ExecutionKind);
    }

    [TestMethod(DisplayName = "create_character 新角色默认仅在被提及时参与")]
    public async Task CreateCharacter_NewRoleDefaultsToMentionOnly()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("test-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "新角色",
                ["systemPrompt"] = "你是一个新角色",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        Assert.HasCount(1, manager.Roles);
        Assert.AreEqual(ChatRoomParticipationMode.MentionOnly, manager.Roles[0].Definition.ParticipationMode);
    }

    [TestMethod(DisplayName = "create_character 角色名为空时应返回错误")]
    public async Task CreateCharacter_WithEmptyRoleName_ReturnsError()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object? resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "",
                ["systemPrompt"] = "测试",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("错误", result);
        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod(DisplayName = "create_character 人设为空时应返回错误")]
    public async Task CreateCharacter_WithEmptySystemPrompt_ReturnsError()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object? resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "测试角色",
                ["systemPrompt"] = "",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("错误", result);
        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod(DisplayName = "list_characters 有角色时应返回格式化表格")]
    public async Task ListCharacters_WithRoles_ReturnsFormattedTable()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        await manager.AddRoleAsync(new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "architect",
            RoleName = "架构师",
            SystemPrompt = "关注系统设计",
        }));

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction listTool = GetTool(tools, "list_characters");

        object? resultObj = await listTool.InvokeAsync(new AIFunctionArguments());
        string result = GetResultString(resultObj);

        Assert.Contains("架构师", result);
        Assert.Contains("architect", result);
    }

    [TestMethod(DisplayName = "list_characters 无角色时应返回空列表提示")]
    public async Task ListCharacters_WithNoRoles_ReturnsEmptyMessage()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction listTool = GetTool(tools, "list_characters");

        object? resultObj = await listTool.InvokeAsync(new AIFunctionArguments());
        string result = GetResultString(resultObj);

        Assert.Contains("没有角色", result);
    }

    [TestMethod(DisplayName = "edit_character 应更新已有角色属性")]
    [Timeout(5000)]
    public async Task EditCharacter_ExistingRole_UpdatesProperties()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "原角色名",
            SystemPrompt = "原人设",
        });
        await manager.AddRoleAsync(role);
        ChatRoomRole? updatedRole = null;
        manager.RoleUpdated += (_, value) => updatedRole = value;

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction editTool = GetTool(tools, "edit_character");

        object? resultObj = await editTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleId"] = "role-1",
                ["roleName"] = "新角色名",
                ["systemPrompt"] = "新人设",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("✅", result);
        Assert.AreSame(role, manager.Roles[0]);
        Assert.AreSame(role, updatedRole);
        Assert.AreEqual("新角色名", manager.Roles[0].Definition.RoleName);
        Assert.AreEqual("新人设", manager.Roles[0].Definition.SystemPrompt);
    }

    [TestMethod(DisplayName = "edit_character 编辑 Coding 角色时应保留执行器和执行种类")]
    [Timeout(5000)]
    public async Task EditCharacter_CodingRole_PreservesRuntimeAndExecutionKind()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var executor = new TestRoleExecutor(ChatRoomRoleExecutionKind.Coding);
        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "coding-role",
            ExecutionKind = ChatRoomRoleExecutionKind.Coding,
            RoleName = "编程角色",
            SystemPrompt = string.Empty,
        }, null, executor);
        await manager.AddRoleAsync(role);

        AIFunction editTool = GetTool(ChatRoomRoleManagementTools.CreateTools(manager), "edit_character");
        object? resultObj = await editTool.InvokeAsync(new AIFunctionArguments
        {
            ["roleId"] = "coding-role",
            ["roleName"] = "高级编程角色",
            ["systemPrompt"] = null,
            ["modelId"] = null,
            ["modelProviderId"] = null,
            ["memoryContent"] = "保留上下文",
        });

        Assert.Contains("✅", GetResultString(resultObj));
        Assert.AreSame(role, manager.Roles.Single());
        Assert.AreSame(executor, role.Executor);
        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, role.Definition.ExecutionKind);
        Assert.IsFalse(role.Definition.IsHuman);
        Assert.AreEqual("保留上下文", role.Definition.MemoryContent);
    }

    [TestMethod(DisplayName = "edit_character 角色不存在时应返回错误")]
    public async Task EditCharacter_NonExistentRole_ReturnsError()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction editTool = GetTool(tools, "edit_character");

        object? resultObj = await editTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleId"] = "non-existent",
                ["roleName"] = "新名",
                ["systemPrompt"] = null,
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("❌", result);
    }

    [TestMethod(DisplayName = "create_character 仅指定提供商时应保留提供商标识")]
    public async Task CreateCharacter_WithOnlyModelProviderId_PreservesProviderId()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("custom-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["custom-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "指定提供商",
                ["systemPrompt"] = "你是一个指定了提供商的角色",
                ["modelId"] = null,
                ["modelProviderId"] = "custom-provider",
                ["memoryContent"] = null,
            });

        Assert.HasCount(1, manager.Roles);
        Assert.AreEqual("custom-provider", manager.Roles[0].Definition.ModelProviderId);
        Assert.IsNull(manager.Roles[0].Definition.ModelId);
    }

    [TestMethod(DisplayName = "create_character 同时指定模型和提供商时应保留两者")]
    public async Task CreateCharacter_WithBothModelIdAndProviderId_PreservesBoth()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("custom-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["custom-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "指定模型",
                ["systemPrompt"] = "你是一个指定了模型的角色",
                ["modelId"] = "test-model",
                ["modelProviderId"] = "custom-provider",
                ["memoryContent"] = null,
            });

        Assert.HasCount(1, manager.Roles);
        Assert.AreEqual("custom-provider", manager.Roles[0].Definition.ModelProviderId);
        Assert.AreEqual("test-model", manager.Roles[0].Definition.ModelId);
    }

    [TestMethod(DisplayName = "create_character 空白模型配置应视为空值")]
    public async Task CreateCharacter_WithWhitespaceModelProviderId_TreatedAsNull()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("default-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["default-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "空白提供商",
                ["systemPrompt"] = "你是一个测试角色",
                ["modelId"] = "  ",
                ["modelProviderId"] = "  ",
                ["memoryContent"] = null,
            });

        Assert.HasCount(1, manager.Roles);
        Assert.IsNull(manager.Roles[0].Definition.ModelProviderId);
        Assert.IsNull(manager.Roles[0].Definition.ModelId);
    }

    [TestMethod(DisplayName = "create_character 无模型提供商时应失败且不保留角色")]
    public async Task CreateCharacter_WithoutProviders_ReturnsErrorAndRoleNotAdded()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object? resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "无模型角色",
                ["systemPrompt"] = "你是一个没有模型的角色",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("创建角色失败", result);
        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod(DisplayName = "create_character 已注册提供商时应通过模型可用性校验")]
    public async Task CreateCharacter_WithRegisteredProviders_NewRolePassesEnsureModelAvailable()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("test-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "测试角色",
                ["systemPrompt"] = "你是一个测试角色",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        // 新角色应通过 EnsureModelAvailable 校验，不抛异常
        manager.Roles[0].EnsureModelAvailable();
    }

    [TestMethod(DisplayName = "create_character 角色名含空格时返回错误提示且不创建角色")]
    public async Task CreateCharacter_UnparsableRoleName_ReturnsErrorAndRoleNotAdded()
    {
        var manager = new ChatRoomManager();
        Mock<ILanguageModelProvider> mockProvider = CreateMockProvider("test-provider");
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object? resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "Code Expert",
                ["systemPrompt"] = "你是一个测试角色",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("无法被 @ 正确解析", result);
        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod(DisplayName = "edit_character 修改为不可解析角色名时返回错误提示且不修改角色名")]
    public async Task EditCharacter_UnparsableRoleName_ReturnsErrorAndRoleNameUnchanged()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        await manager.AddRoleAsync(new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "原角色名",
            SystemPrompt = "原人设",
        }));

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction editTool = GetTool(tools, "edit_character");

        object? resultObj = await editTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleId"] = "role-1",
                ["roleName"] = "Bad Name",
                ["systemPrompt"] = null,
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = GetResultString(resultObj);
        Assert.Contains("无法被 @ 正确解析", result);
        Assert.AreEqual("原角色名", manager.Roles[0].Definition.RoleName);
    }

    private sealed class TestRoleExecutor(ChatRoomRoleExecutionKind executionKind) : IChatRoomRoleExecutor
    {
        public ChatRoomRoleExecutionKind ExecutionKind { get; } = executionKind;

        public Task<ChatRoomRoleExecutionResult> RunAsync(
            ChatRoomRoleExecutionContext context,
            IReadOnlyList<AIContent> contents,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetWorkspacePathAsync(
            CopilotChatManager chatManager,
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            chatManager.WorkspacePath = workspacePath;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => default;
    }
}
