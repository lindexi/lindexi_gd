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

    [TestMethod]
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

        object resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "代码审查员",
                ["systemPrompt"] = "你是一位资深代码审查员",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = resultObj.ToString()!;
        Assert.Contains("✅", result);
        Assert.HasCount(1, manager.Roles);

        ChatRoomRole newRole = manager.Roles[0];
        Assert.HasCount(1, newRole.EndpointManager.GetSupportedModels());
    }

    [TestMethod]
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

    [TestMethod]
    public async Task CreateCharacter_WithoutRegisteredProviders_ReturnsError()
    {
        var manager = new ChatRoomManager();
        // 未注册任何 providers

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "无模型角色",
                ["systemPrompt"] = "你是一个没有模型的角色",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = resultObj.ToString()!;
        Assert.Contains("创建角色失败", result);
        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod]
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

        object resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "架构师",
                ["systemPrompt"] = "你是一位架构师",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = "熟悉微服务架构",
            });

        string result = resultObj.ToString()!;
        Assert.Contains("✅", result);
        Assert.Contains("架构师", result);
        Assert.HasCount(1, manager.Roles);
        Assert.AreEqual("架构师", manager.Roles[0].Definition.RoleName);
        Assert.IsFalse(manager.Roles[0].Definition.IsHuman);
    }

    [TestMethod]
    public async Task CreateCharacter_WithEmptyRoleName_ReturnsError()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "",
                ["systemPrompt"] = "测试",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = resultObj.ToString()!;
        Assert.Contains("错误", result);
        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod]
    public async Task CreateCharacter_WithEmptySystemPrompt_ReturnsError()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction createTool = GetTool(tools, "create_character");

        object resultObj = await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "测试角色",
                ["systemPrompt"] = "",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = resultObj.ToString()!;
        Assert.Contains("错误", result);
        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod]
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

        object resultObj = await listTool.InvokeAsync(new AIFunctionArguments());
        string result = resultObj.ToString()!;

        Assert.Contains("架构师", result);
        Assert.Contains("architect", result);
    }

    [TestMethod]
    public async Task ListCharacters_WithNoRoles_ReturnsEmptyMessage()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction listTool = GetTool(tools, "list_characters");

        object resultObj = await listTool.InvokeAsync(new AIFunctionArguments());
        string result = resultObj.ToString()!;

        Assert.Contains("没有角色", result);
    }

    [TestMethod]
    public async Task EditCharacter_ExistingRole_UpdatesProperties()
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

        object resultObj = await editTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleId"] = "role-1",
                ["roleName"] = "新角色名",
                ["systemPrompt"] = "新人设",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = resultObj.ToString()!;
        Assert.Contains("✅", result);
        Assert.AreEqual("新角色名", manager.Roles[0].Definition.RoleName);
        Assert.AreEqual("新人设", manager.Roles[0].Definition.SystemPrompt);
    }

    [TestMethod]
    public async Task EditCharacter_NonExistentRole_ReturnsError()
    {
        var manager = new ChatRoomManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction editTool = GetTool(tools, "edit_character");

        object resultObj = await editTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleId"] = "non-existent",
                ["roleName"] = "新名",
                ["systemPrompt"] = null,
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        string result = resultObj.ToString()!;
        Assert.Contains("❌", result);
    }
}
