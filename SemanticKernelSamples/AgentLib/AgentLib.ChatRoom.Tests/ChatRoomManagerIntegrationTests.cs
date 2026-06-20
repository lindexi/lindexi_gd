using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.SpeakerSelectors;
using AgentLib.ChatRoom.Tools;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;

using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom.Tests;

/// <summary>
/// <see cref="ChatRoomManager"/> 的集成测试，使用 <see cref="FakeChatClient"/> 模拟完整 LLM 流程。
/// 重点验证 @mention 死循环 Bug 及相关场景。
/// </summary>
[TestClass]
public sealed class ChatRoomManagerIntegrationTests
{
    /// <summary>
    /// 助手 @ 了 MentionOnly 角色，但该角色 LLM 返回空内容时，循环应在有限步骤内终止，不死循环。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_MentionedRoleReturnsEmpty_LoopTerminatesWithoutDeadLoop()
    {
        // Arrange：helper 发言后 @ expert，expert 的 FakeChatClient 返回空内容
        var helperClient = CreateFakeClient("helper 的回复 @expert");
        var expertClient = CreateEmptyFakeClient();

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["helper-provider"] = new FakeLanguageModelProvider(helperClient),
            ["expert-provider"] = new FakeLanguageModelProvider(expertClient),
        });
        await manager.AddRoleAsync(CreateRole("helper", "Helper", "helper-provider"));
        await manager.AddRoleAsync(CreateRole("expert", "Expert", "expert-provider", ChatRoomParticipationMode.MentionOnly));

        // 使用 RoundRobinSpeakerSelector，MaxRounds 限制防止万一
        manager.SpeakerSelector = new RoundRobinSpeakerSelector { MaxRounds = 5 };

        // 人类插话 @ helper → helper 发言 @ expert → expert 返回空
        await manager.HumanInterjectAsync("@helper 开始讨论", "human", "Human");

        // Act
        await manager.StartAutoLoopAsync();

        // Assert：循环应已终止（不死循环），helper 至少发了一条消息
        Assert.IsFalse(manager.IsRunning);
        var helperMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "helper" && !m.IsSystemMessage)
            .ToList();
        Assert.IsTrue(helperMessages.Count > 0);
    }

    /// <summary>
    /// 被 @ 的角色返回空内容后，应跳过该角色继续选择下一个发言者。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_MentionedRoleReturnsEmpty_ContinuesToNextSpeaker()
    {
        // Arrange：helper @ expert，expert 返回空，analyst 正常发言
        var helperClient = CreateFakeClient("helper 说 @expert 帮忙");
        var expertClient = CreateEmptyFakeClient();
        var analystClient = CreateFakeClient("analyst 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["helper-provider"] = new FakeLanguageModelProvider(helperClient),
            ["expert-provider"] = new FakeLanguageModelProvider(expertClient),
            ["analyst-provider"] = new FakeLanguageModelProvider(analystClient),
        });
        await manager.AddRoleAsync(CreateRole("helper", "Helper", "helper-provider"));
        await manager.AddRoleAsync(CreateRole("expert", "Expert", "expert-provider", ChatRoomParticipationMode.MentionOnly));
        await manager.AddRoleAsync(CreateRole("analyst", "Analyst", "analyst-provider"));

        manager.SpeakerSelector = new RoundRobinSpeakerSelector { MaxRounds = 3 };

        await manager.HumanInterjectAsync("@helper 开始", "human", "Human");

        // Act
        await manager.StartAutoLoopAsync();

        // Assert：analyst 应有发言（expert 空回复后跳过，继续轮流）
        Assert.IsFalse(manager.IsRunning);
        var analystMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "analyst" && !m.IsSystemMessage)
            .ToList();
        Assert.IsTrue(analystMessages.Count > 0);
    }

    /// <summary>
    /// 正常流程：两个 AlwaysParticipate 角色各发言一次后循环结束（MaxRounds=1）。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_NormalFlow_AllRolesSpeak()
    {
        var clientA = CreateFakeClient("A 的回复");
        var clientB = CreateFakeClient("B 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["provider-a"] = new FakeLanguageModelProvider(clientA),
            ["provider-b"] = new FakeLanguageModelProvider(clientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "RoleA", "provider-a"));
        await manager.AddRoleAsync(CreateRole("B", "RoleB", "provider-b"));

        manager.SpeakerSelector = new RoundRobinSpeakerSelector { MaxRounds = 1 };

        // 人类插话触发对话
        await manager.HumanInterjectAsync("开始讨论吧", "human", "Human");

        // Act
        await manager.StartAutoLoopAsync();

        // Assert
        Assert.IsFalse(manager.IsRunning);
        var aMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "A" && !m.IsSystemMessage)
            .ToList();
        var bMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "B" && !m.IsSystemMessage)
            .ToList();
        Assert.IsTrue(aMessages.Count > 0);
        Assert.IsTrue(bMessages.Count > 0);
    }

    /// <summary>
    /// @ 链式调用：A @ B → B 发言 → B @ C → C 发言 → 循环继续。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_MentionChain_AllMentionedRolesSpeak()
    {
        // A 发言并 @ B，B 发言并 @ C，C 正常发言
        var clientA = CreateFakeClient("A 说 @B");
        var clientB = CreateFakeClient("B 说 @C");
        var clientC = CreateFakeClient("C 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = new FakeLanguageModelProvider(clientA),
            ["p-b"] = new FakeLanguageModelProvider(clientB),
            ["p-c"] = new FakeLanguageModelProvider(clientC),
        });
        await manager.AddRoleAsync(CreateRole("A", "A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "B", "p-b"));
        await manager.AddRoleAsync(CreateRole("C", "C", "p-c"));

        manager.SpeakerSelector = new RoundRobinSpeakerSelector { MaxRounds = 2 };

        // 人类插话触发对话
        await manager.HumanInterjectAsync("开始讨论", "human", "Human");

        // Act
        await manager.StartAutoLoopAsync();

        // Assert：A、B、C 都应有发言
        Assert.IsFalse(manager.IsRunning);
        foreach (var roleId in new[] { "A", "B", "C" })
        {
            var messages = manager.Session.Messages
                .Where(m => m.SenderRoleId == roleId && !m.IsSystemMessage)
                .ToList();
            Assert.IsTrue(messages.Count > 0, $"{roleId} 应有发言");
        }
    }

    // === helper ===

    /// <summary>
    /// 人类插话后，各 AlwaysParticipate 角色各发言一次，然后循环自然暂停。
    /// 验证"人类说话后无尽发言"的 Bug 已修复。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_HumanInterject_AllRolesSpeakOnceThenStop()
    {
        var clientA = CreateFakeClient("A 的回复");
        var clientB = CreateFakeClient("B 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["provider-a"] = new FakeLanguageModelProvider(clientA),
            ["provider-b"] = new FakeLanguageModelProvider(clientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "A", "provider-a"));
        await manager.AddRoleAsync(CreateRole("B", "B", "provider-b"));

        manager.SpeakerSelector = new RoundRobinSpeakerSelector();

        // 人类插话触发对话
        await manager.HumanInterjectAsync("开始讨论", "human", "Human");

        // Act
        await manager.StartAutoLoopAsync();

        // Assert：循环应已终止（不再无尽发言）
        Assert.IsFalse(manager.IsRunning);

        // A 和 B 各发言恰好一次
        var aMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "A" && !m.IsSystemMessage)
            .ToList();
        var bMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "B" && !m.IsSystemMessage)
            .ToList();
        Assert.AreEqual(1, aMessages.Count);
        Assert.AreEqual(1, bMessages.Count);
    }

    /// <summary>
    /// 安全网阈值基于可发言的非人类角色数量，而非包含人类角色的 Roles.Count。
    /// 当所有可发言角色都连续返回空时，循环应终止。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_AllSpeakableRolesReturnEmpty_TerminatesQuickly()
    {
        // 两个 AlwaysParticipate 角色都返回空
        var emptyClientA = CreateEmptyFakeClient();
        var emptyClientB = CreateEmptyFakeClient();

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = new FakeLanguageModelProvider(emptyClientA),
            ["p-b"] = new FakeLanguageModelProvider(emptyClientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "B", "p-b"));
        // 人类角色不影响安全网阈值
        await manager.AddRoleAsync(CreateRole("human", "Human", "", isHuman: true));

        manager.SpeakerSelector = new RoundRobinSpeakerSelector();

        await manager.HumanInterjectAsync("开始", "human", "Human");

        // Act
        await manager.StartAutoLoopAsync();

        // Assert：循环应已终止（安全网阈值为 2 个可发言角色，不死循环）
        Assert.IsFalse(manager.IsRunning);
    }

    private static ChatRoomManager CreateManager()
    {
        return new ChatRoomManager();
    }

    /// <summary>
    /// 模拟角色通过 create_character 工具创建新角色后，新角色被 @ 时能正常发言。
    /// 验证 CreateCharacter 添加的新角色模型注册正确，EnsureModelAvailable 不抛异常。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task CreateCharacterTool_ThenMentionNewRole_NewRoleSpeaksSuccessfully()
    {
        // helper 发言后会 @ "新角色"（与 create_character 创建的角色名一致）
        var helperClient = CreateFakeClient("helper 的回复 @[新角色]");
        // 新角色的 FakeChatClient，在被 @ 时返回有效内容
        var newRoleClient = CreateFakeClient("新角色的回复");

        var manager = CreateManager();
        // 注册所有 provider（helper 和新角色都会用到）
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["helper-provider"] = new FakeLanguageModelProvider(helperClient),
            ["newrole-provider"] = new FakeLanguageModelProvider(newRoleClient),
        });
        await manager.AddRoleAsync(CreateRole("helper", "Helper", "helper-provider"));

        // 模拟角色调用 create_character 工具创建新角色
        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction? createTool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Name == "create_character");
        Assert.IsNotNull(createTool, "未找到 create_character 工具");

        await createTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["roleName"] = "新角色",
                ["systemPrompt"] = "你是一个新角色",
                ["modelId"] = null,
                ["modelProviderId"] = null,
                ["memoryContent"] = null,
            });

        // 验证新角色已创建且为 MentionOnly 模式
        Assert.HasCount(2, manager.Roles);
        ChatRoomRole newRole = manager.Roles.First(r => r.Definition.RoleName == "新角色");
        Assert.AreEqual(ChatRoomParticipationMode.MentionOnly, newRole.Definition.ParticipationMode);

        // 新角色应有可用模型（EnsureModelAvailable 不抛异常）
        newRole.EnsureModelAvailable();

        // 人类 @ helper → helper 发言 @ [新角色] → 新角色被 @ 后发言
        manager.SpeakerSelector = new RoundRobinSpeakerSelector { MaxRounds = 3 };

        await manager.HumanInterjectAsync("@helper 开始讨论", "human", "Human");
        await manager.StartAutoLoopAsync();

        // 验证循环正常终止，未因 EnsureModelAvailable 抛出异常
        Assert.IsFalse(manager.IsRunning);

        // helper 应有发言
        var helperMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "helper" && !m.IsSystemMessage)
            .ToList();
        Assert.IsTrue(helperMessages.Count > 0, "helper 应有发言");

        // 新角色应有发言（被 @ 后正常发言，而非因 EnsureModelAvailable 失败）
        var newRoleMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == newRole.Definition.RoleId && !m.IsSystemMessage)
            .ToList();
        Assert.IsTrue(newRoleMessages.Count > 0, "新角色被 @ 后应有发言");

        // 不应有发言失败的系统消息
        var failedMessages = manager.Session.Messages
            .Where(m => m.IsSystemMessage && (m.Content?.Contains("发言失败") == true))
            .ToList();
        Assert.HasCount(0, failedMessages, "不应有发言失败的系统消息");
    }

    /// <summary>
    /// 未注册任何模型提供商时调用 create_character 工具，应返回错误信息且角色不被添加。
    /// </summary>
    [TestMethod]
    public async Task CreateCharacterTool_WithoutProviders_ReturnsErrorAndRoleNotAdded()
    {
        var manager = CreateManager();

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction? createTool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Name == "create_character");
        Assert.IsNotNull(createTool, "未找到 create_character 工具");

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

    /// <summary>
    /// create_character 工具应将新角色默认设为 MentionOnly 模式。
    /// </summary>
    [TestMethod]
    public async Task CreateCharacterTool_NewRoleDefaultsToMentionOnly()
    {
        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = new FakeLanguageModelProvider(CreateFakeClient("测试回复")),
        });

        IReadOnlyList<AITool> tools = ChatRoomRoleManagementTools.CreateTools(manager);
        AIFunction? createTool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Name == "create_character");
        Assert.IsNotNull(createTool, "未找到 create_character 工具");

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

    private static ChatRoomRole CreateRole(
        string roleId,
        string roleName,
        string modelProviderId,
        ChatRoomParticipationMode mode = ChatRoomParticipationMode.AlwaysParticipate,
        bool isHuman = false)
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = roleId,
            RoleName = roleName,
            IsHuman = isHuman,
            ParticipationMode = mode,
            ModelProviderId = string.IsNullOrEmpty(modelProviderId) ? null : modelProviderId,
        };

        return new ChatRoomRole(definition);
    }

    /// <summary>
    /// 创建返回指定文本的 <see cref="FakeChatClient"/>。
    /// </summary>
    private static FakeChatClient CreateFakeClient(string responseText)
    {
        var client = new FakeChatClient();

        client.OnGetStreamingResponseAsync = (_, _, _) => StreamUpdates(
        [
            new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent(responseText)],
            },
        ]);

        client.OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, responseText)));

        return client;
    }

    /// <summary>
    /// 创建返回空内容的 <see cref="FakeChatClient"/>，模拟角色"没有话要说"。
    /// 产生一个 Contents 为空的 <see cref="ChatResponseUpdate"/>，
    /// 触发 ClearMessageItems 但不追加任何文本，使 Content 为空。
    /// </summary>
    private static FakeChatClient CreateEmptyFakeClient()
    {
        var client = new FakeChatClient();

        client.OnGetStreamingResponseAsync = (_, _, _) => StreamUpdates(
        [
            new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [],
            },
        ]);

        client.OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, string.Empty)));

        return client;
    }

    /// <summary>
    /// 将 <see cref="ChatResponseUpdate"/> 数组转为 <see cref="IAsyncEnumerable{T}"/>。
    /// </summary>
    private static async IAsyncEnumerable<ChatResponseUpdate> StreamUpdates(
        ChatResponseUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }
}
