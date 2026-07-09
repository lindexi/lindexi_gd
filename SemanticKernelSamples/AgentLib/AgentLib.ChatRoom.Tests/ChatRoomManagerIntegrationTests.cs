using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
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
            ["helper-provider"] = CreateProvider("helper-provider", helperClient),
            ["expert-provider"] = CreateProvider("expert-provider", expertClient),
        });
        await manager.AddRoleAsync(CreateRole("helper", "helper-provider"));
        await manager.AddRoleAsync(CreateRole("expert", "expert-provider", ChatRoomParticipationMode.MentionOnly));

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
    /// 被 @ 的角色返回空内容后，应由管理者介入，而不是继续选择无关普通角色。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_MentionedRoleReturnsEmpty_ManagersSpeak()
    {
        // Arrange：helper @ expert，expert 返回空，两个 manager 兜底发言
        var helperClient = CreateFakeClient("helper 说 @expert 帮忙");
        var expertClient = CreateEmptyFakeClient();
        var managerClientA = CreateFakeClient("manager A 的回复");
        var managerClientB = CreateFakeClient("manager B 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["helper-provider"] = CreateProvider("helper-provider", helperClient),
            ["expert-provider"] = CreateProvider("expert-provider", expertClient),
            ["manager-a-provider"] = CreateProvider("manager-a-provider", managerClientA),
            ["manager-b-provider"] = CreateProvider("manager-b-provider", managerClientB),
        });
        await manager.AddRoleAsync(CreateRole("helper", "helper-provider"));
        await manager.AddRoleAsync(CreateRole("expert", "expert-provider", ChatRoomParticipationMode.MentionOnly));
        await manager.AddRoleAsync(CreateRole("manager-a", "manager-a-provider", isManagerRole: true));
        await manager.AddRoleAsync(CreateRole("manager-b", "manager-b-provider", isManagerRole: true));

        await manager.HumanInterjectAsync("@helper 开始", "human", "Human");

        // Act
        await manager.StartAutoLoopAsync();

        // Assert：按调度规则取第一个管理者兜底介入
        Assert.IsFalse(manager.IsRunning);
        var managerMessages = manager.Session.Messages
            .Where(m => (m.SenderRoleId == "manager-a" || m.SenderRoleId == "manager-b") && !m.IsSystemMessage)
            .ToList();
        Assert.AreEqual(1, managerMessages.Count);
        Assert.AreEqual("manager-a", managerMessages[0].SenderRoleId);
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
            ["provider-a"] = CreateProvider("provider-a", clientA),
            ["provider-b"] = CreateProvider("provider-b", clientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "provider-a"));
        await manager.AddRoleAsync(CreateRole("B", "provider-b"));

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
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-c"] = CreateProvider("p-c", clientC),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("C", "p-c"));

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

    /// <summary>
    /// 人类消息显式 @ 多个角色时，应按消息中的 @ 顺序优先发言，且不启动默认队列。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_HumanMentionsMultipleRoles_MentionedRolesSpeakInMentionOrder()
    {
        var clientA = CreateFakeClient("A 的回复");
        var clientB = CreateFakeClient("B 的回复");
        var clientC = CreateFakeClient("C 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-c"] = CreateProvider("p-c", clientC),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b", ChatRoomParticipationMode.MentionOnly));
        await manager.AddRoleAsync(CreateRole("C", "p-c"));

        await manager.HumanInterjectAsync("@B 请先看，@A 接着看", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        CollectionAssert.AreEqual(
            new[] { "B", "A" },
            GetAssistantSenderRoleIds(manager));
    }

    /// <summary>
    /// 人类消息没有 @ 时，所有 AlwaysParticipate 非人类角色应按注册顺序发言，管理者也参与默认队列。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_HumanWithoutMentions_DefaultRolesIncludeManagerInRoleOrder()
    {
        var clientA = CreateFakeClient("A 的回复");
        var clientB = CreateFakeClient("B 的回复");
        var managerClient = CreateFakeClient("管理者的回复");
        var mentionOnlyClient = CreateFakeClient("C 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-manager"] = CreateProvider("p-manager", managerClient),
            ["p-c"] = CreateProvider("p-c", mentionOnlyClient),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("C", "p-c", ChatRoomParticipationMode.MentionOnly));
        await manager.AddRoleAsync(CreateRole("M", "p-manager", isManagerRole: true));

        await manager.HumanInterjectAsync("请讨论这个方案", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        CollectionAssert.AreEqual(
            new[] { "A", "B", "M" },
            GetAssistantSenderRoleIds(manager));
    }

    /// <summary>
    /// 后续角色发言中产生的 @ 应进入优先栈，在默认队列剩余角色之前发言。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_RoleMentionsAnotherRole_MentionedRoleSpeaksBeforeRemainingDefaultRoles()
    {
        var clientA = CreateFakeClient("A 说 @D 请处理");
        var clientB = CreateFakeClient("B 的回复");
        var clientD = CreateFakeClient("D 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-d"] = CreateProvider("p-d", clientD),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("D", "p-d", ChatRoomParticipationMode.MentionOnly));

        await manager.HumanInterjectAsync("开始讨论", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        CollectionAssert.AreEqual(
            new[] { "A", "D", "B" },
            GetAssistantSenderRoleIds(manager));
    }

    /// <summary>
    /// 初始消息 @A @B @C 后，A 再次 @C 时，C 应插队到 B 之前发言。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_PriorityMentionFromCurrentSpeaker_SpeaksBeforePendingMentionRoles()
    {
        var clientA = CreateFakeClient("A 说 @C 请优先处理");
        var clientB = CreateFakeClient("B 的回复");
        var clientC = CreateFakeClient("C 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-c"] = CreateProvider("p-c", clientC),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("C", "p-c"));

        await manager.HumanInterjectAsync("@A @B @C 请依次处理", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        string[] roleIds = GetAssistantSenderRoleIds(manager);
        Assert.IsTrue(roleIds.Length >= 3);
        CollectionAssert.AreEqual(
            new[] { "A", "C", "B" },
            roleIds.Take(3).ToArray());
    }

    /// <summary>
    /// 同一角色在中间有其他角色回复后，应允许在同一轮自动循环中再次发言。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_MentionChainReturnsToPreviousRole_PreviousRoleSpeaksAgain()
    {
        var clientA = CreateSequenceFakeClient("A 第一次 @B 请回复", "A 第二次回复");
        var clientB = CreateFakeClient("B 回复 @A 请继续");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));

        await manager.HumanInterjectAsync("@A 请开始", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        CollectionAssert.AreEqual(
            new[] { "A", "B", "A" },
            GetAssistantSenderRoleIds(manager));
    }

    /// <summary>
    /// 角色 @ 自己时，不应让同一角色连续发言两次。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_RoleMentionsSelf_DoesNotSpeakConsecutively()
    {
        var clientA = CreateFakeClient("A 说 @A 我继续补充");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));

        await manager.HumanInterjectAsync("@A 请开始", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        CollectionAssert.AreEqual(
            new[] { "A" },
            GetAssistantSenderRoleIds(manager));
    }

    /// <summary>
    /// 某个角色达到单轮最大发言次数时，应跳过该角色并交给管理者仲裁。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_RoleReachesMaxSpeakCount_ManagerArbitrates()
    {
        var clientA = CreateFakeClient("A 说 @B 请继续");
        var clientB = CreateFakeClient("B 说 @A 请继续");
        var managerClient = CreateFakeClient("管理者决定暂停");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-manager"] = CreateProvider("p-manager", managerClient),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("M", "p-manager", isManagerRole: true));

        await manager.HumanInterjectAsync("@A 请开始", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);

        var roleIds = GetAssistantSenderRoleIds(manager);
        Assert.AreEqual(5, roleIds.Count(roleId => roleId == "A"));
        Assert.AreEqual(5, roleIds.Count(roleId => roleId == "B"));
        Assert.AreEqual("M", roleIds.Last());
    }

    /// <summary>
    /// A、B、C 三个角色循环相互 @ 时，达到单角色最大发言次数后应由管理者打断。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_ThreeRoleMentionCycle_ManagerInterruptsWhenRoleReachesLimit()
    {
        var clientA = CreateFakeClient("A 说 @B 继续");
        var clientB = CreateFakeClient("B 说 @C 继续");
        var clientC = CreateFakeClient("C 说 @A 继续");
        var managerClient = CreateFakeClient("管理者决定暂停");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-c"] = CreateProvider("p-c", clientC),
            ["p-manager"] = CreateProvider("p-manager", managerClient),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("C", "p-c"));
        await manager.AddRoleAsync(CreateRole("M", "p-manager", isManagerRole: true));

        await manager.HumanInterjectAsync("@A 请开始", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        string[] roleIds = GetAssistantSenderRoleIds(manager);
        Assert.AreEqual(5, roleIds.Count(roleId => roleId == "A"));
        Assert.AreEqual(5, roleIds.Count(roleId => roleId == "B"));
        Assert.AreEqual(5, roleIds.Count(roleId => roleId == "C"));
        Assert.AreEqual("M", roleIds.Last());
    }

    /// <summary>
    /// 管理者在超限仲裁时重新 @A，应允许 A 重置计数后继续参与后续多轮对话。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_ManagerReassignsLimitedRoleToA_AllowsAContinueMultipleTurns()
    {
        var clientA = CreateFakeClient("A 说 @B 继续");
        var clientB = CreateFakeClient("B 说 @C 继续");
        var clientC = CreateFakeClient("C 说 @A 继续");
        var managerClient = CreateSequenceFakeClient(
            "管理者说 @A 可以继续",
            "管理者说 @B 可以继续",
            "管理者说 @C 可以继续",
            "管理者决定暂停");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-c"] = CreateProvider("p-c", clientC),
            ["p-manager"] = CreateProvider("p-manager", managerClient),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("C", "p-c"));
        await manager.AddRoleAsync(CreateRole("M", "p-manager", isManagerRole: true));

        await manager.HumanInterjectAsync("@A 请开始", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        string[] roleIds = GetAssistantSenderRoleIds(manager);
        Assert.IsTrue(roleIds.Count(roleId => roleId == "A") > 5);
        Assert.IsTrue(roleIds.Count(roleId => roleId == "B") > 5);
        Assert.IsTrue(roleIds.Count(roleId => roleId == "C") > 5);
        Assert.IsTrue(roleIds.Count(roleId => roleId == "M") >= 4);
    }

    /// <summary>
    /// 管理者在超限仲裁时 @D，应将对话指派给 D 继续。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_ManagerReassignsToD_DContinuesConversation()
    {
        var clientA = CreateFakeClient("A 说 @B 继续");
        var clientB = CreateFakeClient("B 说 @C 继续");
        var clientC = CreateFakeClient("C 说 @A 继续");
        var managerClient = CreateFakeClient("管理者说 @D 请接手");
        var clientD = CreateFakeClient("D 接手处理");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
            ["p-c"] = CreateProvider("p-c", clientC),
            ["p-manager"] = CreateProvider("p-manager", managerClient),
            ["p-d"] = CreateProvider("p-d", clientD),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        await manager.AddRoleAsync(CreateRole("C", "p-c"));
        await manager.AddRoleAsync(CreateRole("M", "p-manager", isManagerRole: true));
        await manager.AddRoleAsync(CreateRole("D", "p-d", ChatRoomParticipationMode.MentionOnly));

        await manager.HumanInterjectAsync("@A 请开始", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        string[] roleIds = GetAssistantSenderRoleIds(manager);
        int managerIndex = Array.IndexOf(roleIds, "M");
        Assert.IsTrue(managerIndex >= 0);
        Assert.IsTrue(managerIndex + 1 < roleIds.Length);
        Assert.AreEqual("D", roleIds[managerIndex + 1]);
    }

    /// <summary>
    /// 没有普通角色可发言时，管理者兜底；如果管理者 @ 其他角色，则继续推进链式对话。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_IdleManagerMentionsRole_ChainContinues()
    {
        var clientA = CreateFakeClient("A 完成回复");
        var managerClient = CreateFakeClient("管理者说 @B 请补充");
        var clientB = CreateFakeClient("B 的补充");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-manager"] = CreateProvider("p-manager", managerClient),
            ["p-b"] = CreateProvider("p-b", clientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("M", "p-manager", isManagerRole: true));
        await manager.AddRoleAsync(CreateRole("B", "p-b", ChatRoomParticipationMode.MentionOnly));

        await manager.HumanInterjectAsync("@A 请开始", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        CollectionAssert.AreEqual(
            new[] { "A", "M", "B" },
            GetAssistantSenderRoleIds(manager));
    }

    /// <summary>
    /// 当 trigger 消息只 @ 到人类角色而没有任何非人类角色可发言时，应由管理者兜底发言。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_NoSpeakableRoleFromTrigger_ManagerFallbackSpeaks()
    {
        var managerClient = CreateFakeClient("管理者兜底回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-manager"] = CreateProvider("p-manager", managerClient),
        });
        await manager.AddRoleAsync(CreateRole("human", "", isHuman: true));
        await manager.AddRoleAsync(CreateRole("M", "p-manager", isManagerRole: true));

        await manager.HumanInterjectAsync("@human 请确认", "human", "Human");

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        CollectionAssert.AreEqual(
            new[] { "M" },
            GetAssistantSenderRoleIds(manager));
    }

    // === 插话即时响应测试 ===

    /// <summary>
    /// 自动循环运行期间用户插话时，当前正在发言的角色继续说完，
    /// 随后助手立即回话用户，本轮自动循环结束后验证结果。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_InterjectDuringSpeaking_AssistantRepliesImmediately()
    {
        // Arrange：A 第一轮发言延迟 300ms，使测试能在发言期间插话
        int aCallCount = 0;
        var clientA = new FakeChatClient();
        clientA.OnGetStreamingResponseAsync = (_, _, _) =>
        {
            int index = System.Threading.Interlocked.Increment(ref aCallCount);
            if (index == 1)
            {
                // 第一轮：延迟返回，确保插话在发言期间发生
                return DelayedStreamUpdates(TimeSpan.FromMilliseconds(300),
                [
                    new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent("A 第一轮回复")],
                    },
                ]);
            }
            return StreamUpdates(
            [
                new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new TextContent("A 第二轮回复")],
                },
            ]);
        };
        clientA.OnGetResponseAsync = (_, _, _) =>
        {
            int index = Math.Max(1, aCallCount);
            string text = index == 1 ? "A 第一轮回复" : "A 第二轮回复";
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, text)));
        };
        var clientB = CreateFakeClient("B 第一轮回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-b"] = CreateProvider("p-b", clientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));

        // 人类插话触发对话
        await manager.HumanInterjectAsync("开始讨论", "human", "Human");

        // Act：在后台启动自动循环
        var loopTask = Task.Run(() => manager.StartAutoLoopAsync());

        // 等待 A 开始发言（A 第一轮有 300ms 延迟）
        await Task.Delay(100);

        // 在 A 发言期间插话
        await manager.HumanInterjectAsync("我有新问题", "human", "Human");

        // 等待循环完成
        await loopTask;

        // Assert：循环应已终止
        Assert.IsFalse(manager.IsRunning);

        // A 的第一轮发言应存在（发言期间插话不取消）
        var aMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "A" && !m.IsSystemMessage)
            .ToList();
        Assert.IsTrue(aMessages.Count > 0, "A 的第一轮发言应存在");

        // 人类插话消息应存在
        var humanMessages = manager.Session.Messages
            .Where(m => m.IsHumanMessage)
            .ToList();
        Assert.AreEqual(2, humanMessages.Count, "应有两条人类消息");

        // 插话后 A 应再次发言（助手回话用户）
        Assert.IsTrue(aMessages.Count >= 2, "A 应在插话后再次发言（回话用户）");
    }

    /// <summary>
    /// 自动循环运行期间用户插话，助手回话后 @ 的角色应继续发言（链式继续）。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_InterjectDuringSpeaking_AssistantMentionsRole_ChainContinues()
    {
        // Arrange：A 第一轮发言延迟 300ms，使测试能在发言期间插话
        // A 第二轮回话用户时 @ expert
        int aCallCount = 0;
        var clientA = new FakeChatClient();
        clientA.OnGetStreamingResponseAsync = (_, _, _) =>
        {
            int index = System.Threading.Interlocked.Increment(ref aCallCount);
            if (index == 1)
            {
                // 第一轮：延迟返回，确保插话在发言期间发生
                return DelayedStreamUpdates(TimeSpan.FromMilliseconds(300),
                [
                    new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent("A 第一轮回复")],
                    },
                ]);
            }
            // 第二轮：回话用户并 @ expert
            return StreamUpdates(
            [
                new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new TextContent("A 回复用户 @expert")],
                },
            ]);
        };
        clientA.OnGetResponseAsync = (_, _, _) =>
        {
            int index = Math.Max(1, aCallCount);
            string text = index == 1 ? "A 第一轮回复" : "A 回复用户 @expert";
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, text)));
        };
        var clientExpert = CreateFakeClient("expert 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["p-a"] = CreateProvider("p-a", clientA),
            ["p-expert"] = CreateProvider("p-expert", clientExpert),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("expert", "p-expert", ChatRoomParticipationMode.MentionOnly));

        // 人类插话触发对话
        await manager.HumanInterjectAsync("开始讨论", "human", "Human");

        // Act：在后台启动自动循环
        var loopTask = Task.Run(() => manager.StartAutoLoopAsync());

        // 等待 A 开始发言（A 第一轮有 300ms 延迟）
        await Task.Delay(100);

        // 在 A 发言期间插话
        await manager.HumanInterjectAsync("帮我找专家", "human", "Human");

        // 等待循环完成
        await loopTask;

        // Assert：expert 应被 @ 后发言
        Assert.IsFalse(manager.IsRunning);
        var expertMessages = manager.Session.Messages
            .Where(m => m.SenderRoleId == "expert" && !m.IsSystemMessage)
            .ToList();
        Assert.IsTrue(expertMessages.Count > 0, "expert 应被 @ 后发言");
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
            ["provider-a"] = CreateProvider("provider-a", clientA),
            ["provider-b"] = CreateProvider("provider-b", clientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "provider-a"));
        await manager.AddRoleAsync(CreateRole("B", "provider-b"));

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
    /// 最新非人类消息没有 @ 且没有其他角色可发言时，应触发管理者兜底。
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task StartAutoLoopAsync_NonHumanMessageWithoutMentions_InvokesIdleManagerFallback()
    {
        var managerClient = CreateFakeClient("manager 的回复");

        var manager = CreateManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["manager-provider"] = CreateProvider("manager-provider", managerClient),
        });
        await manager.AddRoleAsync(CreateRole("manager", "manager-provider", isManagerRole: true));

        await manager.Session.AddMessageAsync(ChatRoomMessage.CreateAssistant("已有 AI 消息", "assistant", "Assistant"));

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        Assert.AreEqual(2, manager.Session.Messages.Count);
        Assert.IsTrue(manager.Session.Messages.Any(m => m.SenderRoleId == "manager"));
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
            ["p-a"] = CreateProvider("p-a", emptyClientA),
            ["p-b"] = CreateProvider("p-b", emptyClientB),
        });
        await manager.AddRoleAsync(CreateRole("A", "p-a"));
        await manager.AddRoleAsync(CreateRole("B", "p-b"));
        // 人类角色不影响安全网阈值
        await manager.AddRoleAsync(CreateRole("human", "", isHuman: true));

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
            ["helper-provider"] = CreateProvider("helper-provider", helperClient),
            ["newrole-provider"] = CreateProvider("newrole-provider", newRoleClient),
        });
        await manager.AddRoleAsync(CreateRole("helper", "helper-provider"));

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
            ["test-provider"] = CreateProvider("test-provider", CreateFakeClient("测试回复")),
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
        string roleName,
        string modelProviderId,
        ChatRoomParticipationMode mode = ChatRoomParticipationMode.AlwaysParticipate,
        bool isHuman = false,
        bool isManagerRole = false)
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = roleName,
            RoleName = roleName,
            IsHuman = isHuman,
            IsManagerRole = isManagerRole,
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
    /// 创建按调用顺序返回指定文本的 <see cref="FakeChatClient"/>。
    /// </summary>
    private static FakeChatClient CreateSequenceFakeClient(params string[] responseTexts)
    {
        var client = new FakeChatClient();
        int callCount = 0;

        client.OnGetStreamingResponseAsync = (_, _, _) =>
        {
            int index = Math.Min(System.Threading.Interlocked.Increment(ref callCount), responseTexts.Length) - 1;
            return StreamUpdates(
            [
                new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new TextContent(responseTexts[index])],
                },
            ]);
        };

        client.OnGetResponseAsync = (_, _, _) =>
        {
            int index = Math.Clamp(callCount - 1, 0, responseTexts.Length - 1);
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, responseTexts[index])));
        };

        return client;
    }

    private static string[] GetAssistantSenderRoleIds(ChatRoomManager manager)
    {
        return manager.Session.Messages
            .Where(m => !m.IsSystemMessage && !m.IsHumanMessage)
            .Select(m => m.SenderRoleId)
            .ToArray();
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

    /// <summary>
    /// 延迟后将 <see cref="ChatResponseUpdate"/> 数组转为 <see cref="IAsyncEnumerable{T}"/>。
    /// 用于模拟发言期间插话场景：延迟期间用户可以插话。
    /// </summary>
    private static async IAsyncEnumerable<ChatResponseUpdate> DelayedStreamUpdates(
        TimeSpan delay,
        ChatResponseUpdate[] updates)
    {
        await Task.Delay(delay);
        foreach (var update in updates)
        {
            yield return update;
        }
    }

    /// <summary>
    /// 创建带有指定提供商名称的 <see cref="FakeLanguageModelProvider"/>，
    /// 使其模型能被 <see cref="ChatRoomManager.RegisterModelProvidersForRole"/> 按 ModelProviderId 匹配。
    /// </summary>
    private static FakeLanguageModelProvider CreateProvider(string providerName, FakeChatClient client)
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
        return new FakeLanguageModelProvider([model]);
    }
}
