using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.SpeakerSelectors;

namespace AgentLib.ChatRoom.Tests.SpeakerSelectors;

[TestClass]
public sealed class RoundRobinSpeakerSelectorTests
{
    [TestMethod]
    public void CurrentRound_DefaultValue_ReturnsZero()
    {
        var selector = new RoundRobinSpeakerSelector();

        Assert.AreEqual(0, selector.CurrentRound);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_RolesIsEmpty_ReturnsNull()
    {
        var selector = new RoundRobinSpeakerSelector();

        var result = await selector.SelectNextSpeakerAsync([], []);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_AllRolesAreHuman_ReturnsNull()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("human1", isHuman: true),
            CreateRole("human2", isHuman: true),
        };

        var result = await selector.SelectNextSpeakerAsync(roles, []);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_AllRolesAreMentionOnly_ReturnsNull()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
            CreateRole("role2", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        var result = await selector.SelectNextSpeakerAsync(roles, []);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_SingleLlmRole_ReturnsNullAfterSpoke()
    {
        // 单个角色发言一次后，本轮不再选中
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[] { CreateRole("role1", isHuman: false) };

        var result1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", result1!.Definition.RoleId);

        selector.OnSpeakerResult(result1, success: true);

        var result2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.IsNull(result2);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_MultipleLlmRoles_CyclesThroughRoles()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
            CreateRole("role3", isHuman: false),
        };

        var result1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        var result2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role2", result2!.Definition.RoleId);
        selector.OnSpeakerResult(result2, success: true);

        var result3 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role3", result3!.Definition.RoleId);
        selector.OnSpeakerResult(result3, success: true);

        // 所有角色都已发言 → 返回 null
        var result4 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.IsNull(result4);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_MixedHumanAndLlmRoles_SkipsHumans()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("human", isHuman: true),
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var result1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        var result2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role2", result2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_SkipsMentionOnlyRolesInNormalCycling()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("role2", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
            CreateRole("role3", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
        };

        var result1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        var result2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role3", result2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_CurrentRound_IncrementsWhenWrappingAround()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var r1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);
        selector.OnSpeakerResult(r1!, success: true);

        var r2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);
        selector.OnSpeakerResult(r2!, success: true);

        // 所有角色已发言 → 返回 null，不再进入新一轮
        var r3 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.IsNull(r3);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_Reset_RestoresInitialState()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[] { CreateRole("role1", isHuman: false) };

        var result1 = await selector.SelectNextSpeakerAsync(roles, []);
        selector.OnSpeakerResult(result1!, success: true);

        // Reset 后应恢复初始状态
        selector.Reset();

        Assert.AreEqual(0, selector.CurrentRound);

        var result = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", result!.Definition.RoleId);
        Assert.AreEqual(1, selector.CurrentRound);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_MaxRoundsReached_ReturnsNull()
    {
        var selector = new RoundRobinSpeakerSelector { MaxRounds = 1 };
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var r1 = await selector.SelectNextSpeakerAsync(roles, []);
        selector.OnSpeakerResult(r1!, success: true);

        var r2 = await selector.SelectNextSpeakerAsync(roles, []);
        selector.OnSpeakerResult(r2!, success: true);

        // 所有角色已发言 → 返回 null
        var result = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_MaxRoundsNotReached_Continues()
    {
        var selector = new RoundRobinSpeakerSelector { MaxRounds = 2 };
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var r1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", r1!.Definition.RoleId);
        selector.OnSpeakerResult(r1, success: true);

        var r2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role2", r2!.Definition.RoleId);
        selector.OnSpeakerResult(r2, success: true);

        // 所有角色已发言 → 返回 null（不会进入第 2 轮）
        var result = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_EmptyHistory_NormalRoundRobin()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var result = await selector.SelectNextSpeakerAsync(roles, []);

        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HistoryWithoutHumanEnd_NormalRoundRobin()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
            CreateRole("role3", isHuman: false),
        };

        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateAssistant("msg1", "role1", "R1"),
            ChatRoomMessage.CreateAssistant("msg2", "role2", "R2"),
        };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_MultipleCallsIncrementRoundCorrectly()
    {
        // 不调用 OnSpeakerResult 时，角色不会被标记为已发言，轮流索引持续推进
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        Assert.AreEqual(0, selector.CurrentRound);

        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);

        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);

        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(2, selector.CurrentRound);
    }

    // === @mention tests ===

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanMentionsSingleRole_ReturnsThatRole()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("helper", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("expert", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        var humanMessage = ChatRoomMessage.CreateHuman("@expert help me", "human", "Human");
        humanMessage.MentionedRoleIds = new[] { "expert" };

        var history = new List<ChatRoomMessage> { humanMessage };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        Assert.AreEqual("expert", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanMentionsMultipleRoles_ReturnsByOrder()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("expert1", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
            CreateRole("expert2", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        var humanMessage = ChatRoomMessage.CreateHuman("@expert1 @expert2 help", "human", "Human");
        humanMessage.MentionedRoleIds = new[] { "expert1", "expert2" };

        var history = new List<ChatRoomMessage> { humanMessage };

        var result1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("expert1", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        // expert1 发言（无 @）
        var expert1Msg = ChatRoomMessage.CreateAssistant("ok", "expert1", "Expert1");
        var history2 = new List<ChatRoomMessage> { humanMessage, expert1Msg };

        var result2 = await selector.SelectNextSpeakerAsync(roles, history2);
        Assert.AreEqual("expert2", result2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanMentionsMultipleRoles_QueuePriorityPreventsLoss()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("A", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
            CreateRole("B", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        var humanMessage = ChatRoomMessage.CreateHuman("@A @B", "human", "Human");
        humanMessage.MentionedRoleIds = new[] { "A", "B" };

        var history = new List<ChatRoomMessage> { humanMessage };

        // 第 1 次：A 从队列出队
        var result1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("A", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        // A 发言（无 @）→ history[^1] 变为 A 的消息
        var aMsg = ChatRoomMessage.CreateAssistant("A reply", "A", "A");
        var history2 = new List<ChatRoomMessage> { humanMessage, aMsg };

        // 第 2 次：队列非空，直接出队 B，不看 history[^1]
        var result2 = await selector.SelectNextSpeakerAsync(roles, history2);
        Assert.AreEqual("B", result2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjectionNoMention_RestartsFromFirstAutoRole()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("A", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("B", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
        };

        // 先正常轮流一轮：A → B
        var r1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("A", r1!.Definition.RoleId);
        selector.OnSpeakerResult(r1, success: true);

        var r2 = await selector.SelectNextSpeakerAsync(roles,
            [ChatRoomMessage.CreateAssistant("A msg", "A", "A")]);
        Assert.AreEqual("B", r2!.Definition.RoleId);
        selector.OnSpeakerResult(r2, success: true);

        // 人类插话（没 @ 任何人）
        var humanMessage = ChatRoomMessage.CreateHuman("hello", "human", "Human");
        var history = new List<ChatRoomMessage> { humanMessage };

        // 人类插话 → 清空已发言集合，重置索引，从 A 重新开始
        var result1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("A", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        // A 发言后
        var aMsg = ChatRoomMessage.CreateAssistant("hi", "A", "A");
        var history2 = new List<ChatRoomMessage> { humanMessage, aMsg };

        // 继续轮流 → B
        var result2 = await selector.SelectNextSpeakerAsync(roles, history2);
        Assert.AreEqual("B", result2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanMentionsMultipleRoles_ContinuesAfterAllReplied()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("A", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
            CreateRole("B", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        var humanMessage = ChatRoomMessage.CreateHuman("@A @B", "human", "Human");
        humanMessage.MentionedRoleIds = new[] { "A", "B" };

        var history = new List<ChatRoomMessage> { humanMessage };

        // A 发言
        var r1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("A", r1!.Definition.RoleId);
        selector.OnSpeakerResult(r1, success: true);

        var aMsg = ChatRoomMessage.CreateAssistant("A reply", "A", "A");
        var history2 = new List<ChatRoomMessage> { humanMessage, aMsg };

        // B 发言
        var r2 = await selector.SelectNextSpeakerAsync(roles, history2);
        Assert.AreEqual("B", r2!.Definition.RoleId);
        selector.OnSpeakerResult(r2, success: true);

        var bMsg = ChatRoomMessage.CreateAssistant("B reply", "B", "B");
        var history3 = new List<ChatRoomMessage> { humanMessage, aMsg, bMsg };

        // 队列空 → 没有 AlwaysParticipate 角色，返回 null
        var result3 = await selector.SelectNextSpeakerAsync(roles, history3);
        Assert.IsNull(result3);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_LlmMentionsMentionOnlyRole_ReturnsThatRole()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("helper", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("expert", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        // LLM @ expert（非人类触发）
        var llmMessage = ChatRoomMessage.CreateAssistant("need @expert", "helper", "Helper");
        llmMessage.MentionedRoleIds = new[] { "expert" };

        // 先让 selector 知道当前在正常轮次中（不从人类插话开始）
        var initResult = await selector.SelectNextSpeakerAsync(roles, []); // helper first
        selector.OnSpeakerResult(initResult!, success: true);

        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateAssistant("prev", "helper", "Helper"),
            llmMessage,
        };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        Assert.AreEqual("expert", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_LlmMentionsMentionOnlyRole_ReturnsToNormalCycleAfter()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("A", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("B", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("expert", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        // A @ expert
        var aMsg = ChatRoomMessage.CreateAssistant("need @expert", "A", "A");
        aMsg.MentionedRoleIds = new[] { "expert" };

        // 先跑一轮让状态初始化
        var initResult = await selector.SelectNextSpeakerAsync(roles, []); // A (index 0)
        selector.OnSpeakerResult(initResult!, success: true);

        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateAssistant("init", "A", "A"),
            aMsg,
        };

        // expert 从队列出队
        var result1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("expert", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        // expert 发言（无 @）
        var expertMsg = ChatRoomMessage.CreateAssistant("expert reply", "expert", "Expert");
        var history2 = new List<ChatRoomMessage> { aMsg, expertMsg };

        // 队列空 + 非人类触发 → 继续正常轮流
        var result2 = await selector.SelectNextSpeakerAsync(roles, history2);
        // 正常轮流：B（A 已发言过，跳过）
        Assert.IsNotNull(result2);
        Assert.AreEqual("B", result2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_LlmMentionsChain_CallsThroughQueue()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("A", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("B", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("C", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
        };

        // A @ B
        var aMsg = ChatRoomMessage.CreateAssistant("hey @B", "A", "A");
        aMsg.MentionedRoleIds = new[] { "B" };

        var initResult = await selector.SelectNextSpeakerAsync(roles, []); // init → A
        selector.OnSpeakerResult(initResult!, success: true);

        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateAssistant("init", "A", "A"),
            aMsg,
        };

        var result1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("B", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        // B @ C（链式）
        var bMsg = ChatRoomMessage.CreateAssistant("hey @C", "B", "B");
        bMsg.MentionedRoleIds = new[] { "C" };

        var history2 = new List<ChatRoomMessage> { aMsg, bMsg };

        var result2 = await selector.SelectNextSpeakerAsync(roles, history2);
        Assert.AreEqual("C", result2!.Definition.RoleId);
    }

    // === 死循环 Bug 复现测试（通过 OnSpeakerResult 反馈模式）===

    [TestMethod]
    [Timeout(5000)]
    public async Task SelectNextSpeakerAsync_MentionedRoleHasNoReply_DoesNotReSelectSameRole()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("helper", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("expert", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        // 助手消息 @ expert
        var helperMsg = ChatRoomMessage.CreateAssistant("need @expert", "helper", "Helper");
        helperMsg.MentionedRoleIds = new[] { "expert" };

        var history = new List<ChatRoomMessage> { helperMsg };

        // 第 1 次：expert 从 @mention 队列出队
        var result1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("expert", result1!.Definition.RoleId);

        // 通知 Selector expert 发言失败
        selector.OnSpeakerResult(result1, success: false);

        // 第 2 次调用：不应再次返回 expert（已标记为失败，避免死循环）
        var result2 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreNotEqual("expert", result2?.Definition.RoleId);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SelectNextSpeakerAsync_MentionedRoleReplies_RetryAllowed()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("helper", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("expert", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        // 第 1 条消息 @ expert
        var msg1 = ChatRoomMessage.CreateAssistant("need @expert", "helper", "Helper");
        msg1.MentionedRoleIds = new[] { "expert" };
        var history1 = new List<ChatRoomMessage> { msg1 };

        // 第 1 次：expert 被选中
        var result1 = await selector.SelectNextSpeakerAsync(roles, history1);
        Assert.AreEqual("expert", result1!.Definition.RoleId);
        selector.OnSpeakerResult(result1, success: true);

        // expert 发言后，helper 再次 @ expert（history[^1] 变了）
        var expertMsg = ChatRoomMessage.CreateAssistant("expert reply", "expert", "Expert");
        var msg2 = ChatRoomMessage.CreateAssistant("@expert again", "helper", "Helper");
        msg2.MentionedRoleIds = new[] { "expert" };
        var history2 = new List<ChatRoomMessage> { msg1, expertMsg, msg2 };

        // 第 2 次：history[^1] 是新消息（非人类），可以再次返回 expert
        var result2 = await selector.SelectNextSpeakerAsync(roles, history2);
        Assert.AreEqual("expert", result2!.Definition.RoleId);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SelectNextSpeakerAsync_LlmMentionsRole_RoleHasNoReply_FallsToNormalCycle()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("A", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("B", isHuman: false, mode: ChatRoomParticipationMode.AlwaysParticipate),
            CreateRole("expert", isHuman: false, mode: ChatRoomParticipationMode.MentionOnly),
        };

        // 先跑一轮让状态初始化
        var initResult = await selector.SelectNextSpeakerAsync(roles, []); // A (index 0)
        selector.OnSpeakerResult(initResult!, success: true);

        // A @ expert
        var aMsg = ChatRoomMessage.CreateAssistant("hey @expert", "A", "A");
        aMsg.MentionedRoleIds = new[] { "expert" };
        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateAssistant("init", "A", "A"),
            aMsg,
        };

        // 第 1 次：expert 从队列出队
        var result1 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.AreEqual("expert", result1!.Definition.RoleId);

        // 通知 Selector expert 发言失败
        selector.OnSpeakerResult(result1, success: false);

        // 第 2 次：不应再次返回 expert，应跳过并回到正常轮流
        var result2 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.IsNotNull(result2);
        Assert.AreNotEqual("expert", result2.Definition.RoleId);
    }

    // === OnSpeakerResult 反馈模式测试 ===

    [TestMethod]
    public void OnSpeakerResult_NullRole_ThrowsArgumentNullException()
    {
        var selector = new RoundRobinSpeakerSelector();

        Assert.ThrowsExactly<ArgumentNullException>(() => selector.OnSpeakerResult(null!, true));
    }

    [TestMethod]
    public async Task OnSpeakerResult_Success_AddsToSpokenSet()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var result1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", result1!.Definition.RoleId);

        selector.OnSpeakerResult(result1, success: true);

        // role1 已发言 → 第 2 次应返回 role2
        var result2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role2", result2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task OnSpeakerResult_Success_AllRolesSpoke_ReturnsNull()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var r1 = await selector.SelectNextSpeakerAsync(roles, []);
        selector.OnSpeakerResult(r1!, success: true);

        var r2 = await selector.SelectNextSpeakerAsync(roles, []);
        selector.OnSpeakerResult(r2!, success: true);

        // 所有角色都已发言 → 返回 null
        var r3 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.IsNull(r3);
    }

    [TestMethod]
    public async Task OnSpeakerResult_Failure_PreventsReSelection()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var r1 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role1", r1!.Definition.RoleId);

        // role1 发言失败
        selector.OnSpeakerResult(r1, success: false);

        // 第 2 次：不应返回 role1（已标记失败），应返回 role2
        var r2 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual("role2", r2!.Definition.RoleId);
    }

    [TestMethod]
    public async Task OnSpeakerResult_HumanMessageClearsSpokenSet()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        // 第一轮：role1 和 role2 都发言
        var r1 = await selector.SelectNextSpeakerAsync(roles, []);
        selector.OnSpeakerResult(r1!, success: true);

        var r2 = await selector.SelectNextSpeakerAsync(roles, []);
        selector.OnSpeakerResult(r2!, success: true);

        // 所有角色都已发言 → 返回 null
        var r3 = await selector.SelectNextSpeakerAsync(roles, []);
        Assert.IsNull(r3);

        // 人类插话 → 清空已发言集合，开始新一轮
        var humanMsg = ChatRoomMessage.CreateHuman("new topic", "human", "Human");
        var history = new List<ChatRoomMessage> { humanMsg };

        var r4 = await selector.SelectNextSpeakerAsync(roles, history);
        Assert.IsNotNull(r4);
        Assert.AreEqual("role1", r4!.Definition.RoleId);
    }

    // === helper ===

    private static ChatRoomRole CreateRole(
        string roleId,
        bool isHuman,
        ChatRoomParticipationMode mode = ChatRoomParticipationMode.AlwaysParticipate)
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = roleId,
            RoleName = roleId,
            IsHuman = isHuman,
            ParticipationMode = mode,
        };

        return new ChatRoomRole(definition);
    }
}