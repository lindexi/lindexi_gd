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

    [TestMethod(DisplayName = "并行工具调用只有部分完成时应只保留已完成调用")]
    public void Repair_WhenParallelCallsArePartiallyCompleted_KeepsOnlyCompletedCall()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                CreateFunctionCall("completed-call"),
                CreateFunctionCall("incomplete-call"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("completed-call", "已完成结果"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        CollectionAssert.AreEqual(new[] { "completed-call" }, GetFunctionCalls(messages).Select(call => call.CallId).ToArray());
    }

    [TestMethod(DisplayName = "并行工具结果中穿插孤立结果时应保留合法结果")]
    public void Repair_WhenOrphanResultIsInterleavedWithParallelResults_KeepsValidResults()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                CreateFunctionCall("call-1"),
                CreateFunctionCall("call-2"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("call-1", "结果 1"),
                new FunctionResultContent("orphan-call", "孤立结果"),
                new FunctionResultContent("call-2", "结果 2"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        CollectionAssert.AreEquivalent(new[] { "call-1", "call-2" }, GetFunctionResults(messages).Select(result => result.CallId).ToArray());
    }

    [TestMethod(DisplayName = "同一消息内的文本应中断工具调用与结果配对")]
    public void Repair_WhenTextSeparatesCallAndResultInSameMessage_RemovesPair()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                CreateFunctionCall("interrupted-call"),
                new TextContent("中间文本"),
                new FunctionResultContent("interrupted-call", "工具结果"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.IsFalse(messages.SelectMany(message => message.Contents)
            .Any(content => content is FunctionCallContent or FunctionResultContent));
    }

    [TestMethod(DisplayName = "同一调用 ID 再次发起并完成时应删除较早的未完成调用")]
    public void Repair_WhenCallIdIsReusedAfterIncompleteCall_KeepsOnlyCompletedOccurrence()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("reused-call")]),
            new ChatMessage(ChatRole.Assistant, "中间文本"),
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("reused-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("reused-call", "有效结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.HasCount(1, GetFunctionCalls(messages));
    }

    [TestMethod(DisplayName = "同一调用 ID 的早期孤立结果不应取代后续合法结果")]
    public void Repair_WhenOrphanResultPrecedesValidPairWithSameId_KeepsValidResult()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("reused-call", "孤立结果")]),
            new ChatMessage(ChatRole.Assistant, "中间文本"),
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("reused-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("reused-call", "有效结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.AreEqual("有效结果", GetFunctionResults(messages).Single().Result);
    }

    [TestMethod(DisplayName = "同一调用 ID 的两组完整调用应修复为单一配对")]
    public void Repair_WhenCompletedCallIdIsRepeated_KeepsSinglePair()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("repeated-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("repeated-call", "第一次结果")]),
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("repeated-call")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("repeated-call", "第二次结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.HasCount(GetFunctionCalls(messages).Length, GetFunctionResults(messages));
    }

    [TestMethod(DisplayName = "待追加更新中的文本应中断跨边界工具调用配对")]
    public void Repair_WhenCollectedUpdateTextSeparatesCallAndResult_RemovesHistoricalCall()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("cross-boundary-call")]),
        ];
        AgentResponseUpdate[] updates =
        [
            new AgentResponseUpdate(ChatRole.Assistant, [new TextContent("中间文本")]),
            new AgentResponseUpdate(ChatRole.Assistant,
                [new FunctionResultContent("cross-boundary-call", "工具结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, updates);

        Assert.IsFalse(GetFunctionCalls(messages).Any());
    }

    [TestMethod(DisplayName = "跨边界并行调用乱序返回时应保留所有配对")]
    public void Repair_WhenParallelCallsSpanHistoryAndUpdates_KeepsAllPairs()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("historical-call")]),
        ];
        AgentResponseUpdate[] updates =
        [
            new AgentResponseUpdate(ChatRole.Assistant, [CreateFunctionCall("update-call")]),
            new AgentResponseUpdate(ChatRole.Assistant,
            [
                new FunctionResultContent("update-call", "更新调用结果"),
                new FunctionResultContent("historical-call", "历史调用结果"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, updates);

        Assert.AreEqual("historical-call", GetFunctionCalls(messages).Single().CallId);
    }

    [TestMethod(DisplayName = "空待追加更新不应中断跨边界工具调用配对")]
    public void Repair_WhenEmptyCollectedUpdateSeparatesCallAndResult_KeepsHistoricalCall()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant, [CreateFunctionCall("cross-boundary-call")]),
        ];
        AgentResponseUpdate[] updates =
        [
            new AgentResponseUpdate(ChatRole.Assistant, []),
            new AgentResponseUpdate(ChatRole.Assistant,
                [new FunctionResultContent("cross-boundary-call", "工具结果")]),
        ];

        ToolCallHistoryRepairer.Repair(messages, updates);

        Assert.AreEqual("cross-boundary-call", GetFunctionCalls(messages).Single().CallId);
    }

    [TestMethod(DisplayName = "仅待追加更新包含调用和结果时不应写入历史")]
    public void Repair_WhenPairExistsOnlyInCollectedUpdates_DoesNotModifyHistory()
    {
        var messages = new List<ChatMessage>();
        AgentResponseUpdate[] updates =
        [
            new AgentResponseUpdate(ChatRole.Assistant,
            [
                CreateFunctionCall("update-only-call"),
                new FunctionResultContent("update-only-call", "工具结果"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, updates);

        Assert.IsEmpty(messages);
    }

    [TestMethod(DisplayName = "工具结果位于普通内容之间时应按内容角色完整拆分")]
    public void Repair_WhenResultIsBetweenTextContents_SplitsRolesInOrder()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                CreateFunctionCall("mixed-call"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("mixed-call", "工具结果"),
                new TextContent("结果后文本"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.AreEqual(
            $"{ChatRole.Assistant}:调用前文本|{ChatRole.Tool}:|{ChatRole.Assistant}:结果后文本",
            string.Join('|', messages.Select(message => $"{message.Role}:{message.Text}")));
    }

    [TestMethod(DisplayName = "复杂工具调用历史重复修复时结果应保持不变")]
    public void Repair_WhenCalledTwice_IsIdempotent()
    {
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                CreateFunctionCall("completed-call"),
                CreateFunctionCall("incomplete-call"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("completed-call", "有效结果"),
                new FunctionResultContent("completed-call", "重复结果"),
                new TextContent("结果后文本"),
            ]),
        ];

        ToolCallHistoryRepairer.Repair(messages, []);
        string firstRepair = DescribeMessages(messages);

        ToolCallHistoryRepairer.Repair(messages, []);

        Assert.AreEqual(firstRepair, DescribeMessages(messages));
    }

    private static string DescribeMessages(IEnumerable<ChatMessage> messages)
    {
        return string.Join('|', messages.Select(message =>
            $"{message.Role}:[{string.Join(',', message.Contents.Select(content => content switch
            {
                FunctionCallContent call => $"Call:{call.CallId}",
                FunctionResultContent result => $"Result:{result.CallId}:{result.Result}",
                TextContent text => $"Text:{text.Text}",
                _ => content.GetType().Name,
            }))}]"));
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
