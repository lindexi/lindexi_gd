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
    public async Task SelectNextSpeakerAsync_RolesIsNull_ReturnsNull()
    {
        var selector = new RoundRobinSpeakerSelector();

        var result = await selector.SelectNextSpeakerAsync(null!, []);

        Assert.IsNull(result);
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
    public async Task SelectNextSpeakerAsync_SingleLlmRole_ReturnsSameRoleEachCall()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[] { CreateRole("role1", isHuman: false) };

        var result1 = await selector.SelectNextSpeakerAsync(roles, []);
        var result2 = await selector.SelectNextSpeakerAsync(roles, []);

        Assert.AreEqual("role1", result1!.Definition.RoleId);
        Assert.AreEqual("role1", result2!.Definition.RoleId);
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
        var result2 = await selector.SelectNextSpeakerAsync(roles, []);
        var result3 = await selector.SelectNextSpeakerAsync(roles, []);
        var result4 = await selector.SelectNextSpeakerAsync(roles, []);

        Assert.AreEqual("role1", result1!.Definition.RoleId);
        Assert.AreEqual("role2", result2!.Definition.RoleId);
        Assert.AreEqual("role3", result3!.Definition.RoleId);
        Assert.AreEqual("role1", result4!.Definition.RoleId);
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
        var result2 = await selector.SelectNextSpeakerAsync(roles, []);

        Assert.AreEqual("role1", result1!.Definition.RoleId);
        Assert.AreEqual("role2", result2!.Definition.RoleId);
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

        // First round: index 0 (role1), round is still 0 at first call... wait let me re-read
        // _currentIndex starts at -1, so first call: (-1+1)%2 = 0, _currentIndex==0, _currentRound becomes 1
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);

        // index 1 (role2), round stays 1
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);

        // wrap: index 0 again, round becomes 2
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(2, selector.CurrentRound);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_Reset_RestoresInitialState()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[] { CreateRole("role1", isHuman: false) };

        await selector.SelectNextSpeakerAsync(roles, []);
        await selector.SelectNextSpeakerAsync(roles, []);

        selector.Reset();

        Assert.AreEqual(0, selector.CurrentRound);

        // After reset, _currentIndex is -1, next call returns role1
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

        // First call: wraps to index 0, round becomes 1
        await selector.SelectNextSpeakerAsync(roles, []);
        // index 1, round stays 1
        await selector.SelectNextSpeakerAsync(roles, []);

        // Wraps back to index 0, round becomes 2 > MaxRounds (1) → returns null
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

        // Round 1
        await selector.SelectNextSpeakerAsync(roles, []); // role1
        await selector.SelectNextSpeakerAsync(roles, []); // role2

        // Wraps: round 2
        var result = await selector.SelectNextSpeakerAsync(roles, []); // role1
        Assert.AreEqual("role1", result!.Definition.RoleId);
        Assert.AreEqual(2, selector.CurrentRound);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjection_RestoresFromPreviousLlmSpeaker()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
            CreateRole("role3", isHuman: false),
        };

        // Normal flow: role1, role2, then human interjects
        await selector.SelectNextSpeakerAsync(roles, []); // role1
        var llmMessage = ChatRoomMessage.CreateAssistant("hello", "role2", "Role2");

        var humanMessage = ChatRoomMessage.CreateHuman("interrupt", "human1", "Human");

        var history = new List<ChatRoomMessage> { llmMessage, humanMessage };

        // Human just interjected after role2 spoke → next should be role3
        var result = await selector.SelectNextSpeakerAsync(roles, history);

        Assert.AreEqual("role3", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjection_NoPreviousLlm_StartsFromBeginning()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        // History has only a human message, no previous LLM
        var humanMessage = ChatRoomMessage.CreateHuman("hello", "human1", "Human");
        var history = new List<ChatRoomMessage> { humanMessage };

        // No previous LLM to restore from → start from role1 (index wraps to 0)
        var result = await selector.SelectNextSpeakerAsync(roles, history);

        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjection_PreviousLlmRoleIdNotFound_StartsFromBeginning()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        // LLM message with a roleId that doesn't match any current LLM role
        var unknownLlmMessage = new ChatRoomMessage
        {
            SenderRoleId = "unknown_role",
            Content = "hello",
            SenderRoleName = "Unknown",
        };

        var humanMessage = ChatRoomMessage.CreateHuman("interrupt", "human1", "Human");
        var history = new List<ChatRoomMessage> { unknownLlmMessage, humanMessage };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        // lastLlmIndex = -1, so _currentIndex stays -1, then (-1+1)%2=0 → role1
        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjection_SystemMessagesAreSkipped()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        var systemMsg = ChatRoomMessage.CreateSystem("system notice");
        var llmMessage = ChatRoomMessage.CreateAssistant("hello", "role1", "Role1");
        var humanMessage = ChatRoomMessage.CreateHuman("interrupt", "human1", "Human");

        var history = new List<ChatRoomMessage>
        {
            llmMessage,
            systemMsg,
            humanMessage,
        };

        // systemMsg is skipped, llmMessage with role1 is found
        // lastLlmIndex = 0 (role1), _currentIndex = 0, then (0+1)%2=1 → role2
        var result = await selector.SelectNextSpeakerAsync(roles, history);

        Assert.AreEqual("role2", result!.Definition.RoleId);
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

        // History ends with an LLM message, not human
        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateAssistant("msg1", "role1", "R1"),
            ChatRoomMessage.CreateAssistant("msg2", "role2", "R2"),
        };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        // Normal round robin: starts from role1
        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjection_OnlyHumanHistory_StartsFromBeginning()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        // Multiple human messages at the end, no LLM in history at all
        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateHuman("msg1", "human1", "H1"),
            ChatRoomMessage.CreateHuman("msg2", "human2", "H2"),
        };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        // No LLM found in history → start from role1
        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjection_OnlySystemBeforeHuman_StartsFromBeginning()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        // History: system message, then human message (no LLM message before human)
        var history = new List<ChatRoomMessage>
        {
            ChatRoomMessage.CreateSystem("system"),
            ChatRoomMessage.CreateHuman("interrupt", "human1", "Human"),
        };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        // System message skipped, no LLM found → start from beginning
        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_HumanInterjection_LastLlmHadEmptyRoleId_StartsFromBeginning()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        // LLM message with empty SenderRoleId
        var llmWithEmptyId = new ChatRoomMessage
        {
            SenderRoleId = string.Empty,
            Content = "hello",
            SenderRoleName = "SomeRole",
        };

        var humanMessage = ChatRoomMessage.CreateHuman("interrupt", "human1", "Human");
        var history = new List<ChatRoomMessage> { llmWithEmptyId, humanMessage };

        var result = await selector.SelectNextSpeakerAsync(roles, history);

        // SenderRoleId is empty → not found → starts from beginning
        Assert.AreEqual("role1", result!.Definition.RoleId);
    }

    [TestMethod]
    public async Task SelectNextSpeakerAsync_MultipleCallsIncrementRoundCorrectly()
    {
        var selector = new RoundRobinSpeakerSelector();
        var roles = new[]
        {
            CreateRole("role1", isHuman: false),
            CreateRole("role2", isHuman: false),
        };

        Assert.AreEqual(0, selector.CurrentRound);

        // First call: index goes from -1 to 0 → round becomes 1
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);

        // index 1, round stays 1
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(1, selector.CurrentRound);

        // wraps to 0 → round becomes 2
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(2, selector.CurrentRound);

        // index 1, round stays 2
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(2, selector.CurrentRound);

        // wraps to 0 → round becomes 3
        await selector.SelectNextSpeakerAsync(roles, []);
        Assert.AreEqual(3, selector.CurrentRound);
    }

    private static ChatRoomRole CreateRole(string roleId, bool isHuman)
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = roleId,
            RoleName = roleId,
            IsHuman = isHuman,
        };

        return new ChatRoomRole(definition);
    }
}