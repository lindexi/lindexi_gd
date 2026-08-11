using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001

namespace AgentLib.Tests;

[TestClass]
public sealed class ToolCallHistoryRepairerTests
{
    [TestMethod(DisplayName = "修复器应删除没有结果的工具调用")]
    public void Repair_WhenFunctionCallHasNoResult_RemovesCall()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                CreateFunctionCall("unpaired-call"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.IsFalse(GetFunctionCalls(messages).Any());
    }

    [TestMethod(DisplayName = "修复器删除未配对调用时应保留同消息普通内容")]
    public void Repair_WhenFunctionCallHasNoResult_PreservesText()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                CreateFunctionCall("unpaired-call"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.AreEqual("调用前文本", messages.Single().Text);
    }

    [TestMethod(DisplayName = "修复器应删除没有调用的工具结果")]
    public void Repair_WhenFunctionResultHasNoCall_RemovesResult()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("orphan-result", "孤立结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.IsFalse(GetFunctionResults(messages).Any());
    }

    [TestMethod(DisplayName = "修复器应只保留同一调用的第一条工具结果")]
    public void Repair_WhenFunctionResultIsDuplicated_KeepsFirstResult()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("duplicate-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("duplicate-call", "第一次结果")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("duplicate-call", "第二次结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.AreEqual("第一次结果", GetFunctionResults(messages).Single().Result);
    }

    [TestMethod(DisplayName = "修复器应保留相邻的工具调用和结果")]
    public void Repair_WhenFunctionCallAndResultAreAdjacent_KeepsPair()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("paired-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("paired-call", "工具结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.AreEqual("paired-call", GetFunctionCalls(messages).Single().CallId);
    }

    [TestMethod(DisplayName = "修复器应返回具有结果的工具调用 ID")]
    public void Repair_WhenFunctionCallAndResultArePaired_ReturnsCompletedCallId()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("paired-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("paired-call", "工具结果")]),
        ];

        HashSet<string> completedCallIds = ToolCallHistoryRepairer.Repair(messages, []);

        CollectionAssert.AreEquivalent(new[] { "paired-call" }, completedCallIds.ToArray());
    }

    [TestMethod(DisplayName = "修复器应保留并行调用的乱序相邻结果")]
    public void Repair_WhenParallelFunctionResultsAreOutOfOrder_KeepsAllPairs()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                CreateFunctionCall("parallel-call-1"),
                CreateFunctionCall("parallel-call-2"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("parallel-call-2", "第二个结果"),
                new FunctionResultContent("parallel-call-1", "第一个结果"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.HasCount(2, GetFunctionResults(messages));
    }

    [TestMethod(DisplayName = "修复器应删除不紧邻工具调用的结果")]
    public void Repair_WhenFunctionResultIsNotAdjacent_RemovesPair()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("not-adjacent-call")]),
            new ChatMessage(ChatRole.Assistant, "中间文本"),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("not-adjacent-call", "工具结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.IsFalse(GetFunctionResults(messages).Any());
    }

    [TestMethod(DisplayName = "修复器应将工具结果规范为 Tool 角色")]
    public void Repair_WhenFunctionResultUsesAssistantRole_ChangesRoleToTool()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("role-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("role-call", "工具结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.AreEqual(ChatRole.Tool, messages.Single(message => message.Contents.OfType<FunctionResultContent>().Any()).Role);
    }

    [TestMethod(DisplayName = "修复器拆分工具结果角色时应保留后续普通内容顺序")]
    public void Repair_WhenFunctionResultIsFollowedByText_PreservesContentOrder()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("mixed-call")]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("mixed-call", "工具结果"),
                new TextContent("结果后文本"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        ChatMessage[] repairedMessages = [.. messages.Skip(1)];
        Assert.AreEqual(
            $"{ChatRole.Tool}:|{ChatRole.Assistant}:结果后文本",
            string.Join('|', repairedMessages.Select(message => $"{message.Role}:{message.Text}")));
    }

    [TestMethod(DisplayName = "修复器应使用待追加更新完成历史末尾的工具调用")]
    public void Repair_WhenResultExistsInCollectedUpdates_KeepsHistoricalCall()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("cross-boundary-call")]),
        ];
        AgentResponseUpdate[] updates =
        [
            new AgentResponseUpdate(ChatRole.Assistant,
            [
                new FunctionResultContent("cross-boundary-call", "工具结果"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, updates);

        Assert.AreEqual("cross-boundary-call", GetFunctionCalls(messages).Single().CallId);
    }

    [TestMethod(DisplayName = "修复器应返回待追加更新中的已完成调用 ID")]
    public void Repair_WhenResultExistsInCollectedUpdates_ReturnsCompletedCallId()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("cross-boundary-call")]),
        ];
        AgentResponseUpdate[] updates =
        [
            new AgentResponseUpdate(ChatRole.Assistant,
            [
                new FunctionResultContent("cross-boundary-call", "工具结果"),
            ]),
        ];

        HashSet<string> completedCallIds = ToolCallHistoryRepairer.Repair(messages, updates);

        CollectionAssert.AreEquivalent(new[] { "cross-boundary-call" }, completedCallIds.ToArray());
    }

    private static FunctionCallContent CreateFunctionCall(string callId)
    {
        return new FunctionCallContent(callId, "ReadFileLines", new Dictionary<string, object?>());
    }

    private static FunctionCallContent[] GetFunctionCalls(IEnumerable<ChatMessage> messages)
    {
        return [.. messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>()];
    }

    private static FunctionResultContent[] GetFunctionResults(IEnumerable<ChatMessage> messages)
    {
        return [.. messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()];
    }
}
