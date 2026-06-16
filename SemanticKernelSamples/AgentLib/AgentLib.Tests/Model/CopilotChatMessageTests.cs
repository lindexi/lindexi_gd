using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Model;

[TestClass]
public class CopilotChatMessageTests
{
    [TestMethod]
    [Description("追加文本时应与最后一个文本片段合并并更新正文内容")]
    public void AppendText_WhenLastItemIsText_MergesIntoSingleTextItem()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "Hello");

        message.AppendText(" World");

        Assert.HasCount(1, message.MessageItems);
        Assert.AreEqual("Hello World", message.Content);
    }

    [TestMethod]
    [Description("追加思考内容时应与最后一个思考片段合并并更新思考内容")]
    public void AppendReasoning_WhenLastItemIsReasoning_MergesIntoSingleReasoningItem()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);

        message.AppendReasoning("先分析");
        message.AppendReasoning("再总结");

        Assert.HasCount(1, message.MessageItems);
        Assert.AreEqual("先分析再总结", message.Reason);
        Assert.IsTrue(message.HasReason);
    }

    [TestMethod]
    [Description("追加工具调用与结果时应生成完整的工具片段输出")]
    public void AppendFunctionCallAndResult_WhenUsingRegularTool_CreatesToolItemWithInputAndOutput()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        var functionCall = new FunctionCallContent("call-1", "ReadFile", arguments: new Dictionary<string, object?>
        {
            ["Path"] = "a.txt"
        });
        var functionResult = new FunctionResultContent("call-1", result: new { Success = true });

        message.AppendFunctionCall(functionCall);
        message.AppendFunctionResult(functionResult);

        Assert.HasCount(1, message.MessageItems);
        StringAssert.Contains(message.FullContent, "工具：ReadFile");
        StringAssert.Contains(message.FullContent, "a.txt");
        StringAssert.Contains(message.FullContent, "Success");
    }

    [TestMethod]
    [Description("创建审批工具片段后应保留在消息片段集合中并可按调用 Id 复用")]
    public void CreateApprovalToolItem_WhenCreated_AddsAndReusesApprovalItem()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);

        CopilotChatApprovalToolItem first = message.CreateApprovalToolItem("DeleteFile", "demo.txt", "需要审批", "approval-1");
        CopilotChatApprovalToolItem second = message.CreateApprovalToolItem("DeleteFile", "demo.txt", "需要审批", "approval-1");

        Assert.AreSame(first, second);
        Assert.HasCount(1, message.MessageItems);
        Assert.IsInstanceOfType<CopilotChatApprovalToolItem>(message.MessageItems[0]);
    }

    [TestMethod]
    [Description("追加子智能体调用与结果时应生成包含进度与输出的子代理片段 — SubAgentToolProvider 通过 CreateSubAgentItem 创建项")]
    public void AppendFunctionCallAndResult_WhenUsingSubAgent_CreatesSubAgentItemViaCreateSubAgentItem()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        var functionCall = new FunctionCallContent("sub-1", "InvokeSubAgent", arguments: new Dictionary<string, object?>
        {
            ["Prompt"] = "翻译"
        });

        // AppendFunctionCall 不再创建项，仅跟踪 callId
        message.AppendFunctionCall(functionCall);

        // SubAgentToolProvider 使用自己的 callId 创建子代理项
        CopilotChatSubAgentItem subAgentItem = message.CreateSubAgentItem("调用子智能体", "翻译", "sub-agent-1");
        subAgentItem.AppendText("处理中");

        Assert.HasCount(1, message.MessageItems);
        Assert.IsInstanceOfType<CopilotChatSubAgentItem>(message.MessageItems[0]);
        StringAssert.Contains(message.FullContent, "调用子智能体");
        StringAssert.Contains(message.FullContent, "处理中");
    }

    [TestMethod]
    [Description("助手消息追加用量明细后应生成中文汇总文本")]
    public void AppendUsageDetails_WhenAssistantMessageHasUsage_GeneratesUsageSummary()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "答案");
        var usageDetails = new UsageDetails
        {
            TotalTokenCount = 100,
            InputTokenCount = 40,
            OutputTokenCount = 30,
            ReasoningTokenCount = 20,
            CachedInputTokenCount = 10
        };

        message.AppendUsageDetails(usageDetails);

        Assert.IsTrue(message.HasUsageDetails);
        StringAssert.Contains(message.UsageSummaryText, "用量");
        StringAssert.Contains(message.UsageSummaryText, "总计 100");
        StringAssert.Contains(message.UsageSummaryText, "输入 40");
        StringAssert.Contains(message.UsageSummaryText, "输出 30");
        StringAssert.Contains(message.UsageSummaryText, "思考 20");
        StringAssert.Contains(message.UsageSummaryText, "缓存 10");
    }

    [TestMethod]
    [Description("清空消息片段后应移除正文与思考内容")]
    public void ClearMessageItems_WhenCalled_ClearsDerivedContent()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "原始内容");
        message.AppendReasoning("原始思考");

        message.ClearMessageItems();

        Assert.HasCount(0, message.MessageItems);
        Assert.IsEmpty(message.Content);
        Assert.IsEmpty(message.Reason);
        Assert.IsFalse(message.HasContent);
        Assert.IsFalse(message.HasReason);
    }

    [TestMethod]
    [Description("InvokeSubAgent 的函数调用不应在 MessageItems 中直接创建子代理项，以避免与 SubAgentToolProvider 重复")]
    public void AppendFunctionCall_WhenNameIsInvokeSubAgent_SkipsAddingToMessageItems()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        var functionCall = new FunctionCallContent("sub-call-1", "InvokeSubAgent", arguments: new Dictionary<string, object?>
        {
            ["Prompt"] = "翻译"
        });

        message.AppendFunctionCall(functionCall);

        Assert.HasCount(0, message.MessageItems, "InvokeSubAgent 不应在 MessageItems 中创建项");
    }

    [TestMethod]
    [Description("InvokeSubAgent 的函数结果不应在 MessageItems 中创建工具项")]
    public void AppendFunctionResult_WhenInvokeSubAgentCallId_SkipsAddingToMessageItems()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        var functionCall = new FunctionCallContent("sub-call-1", "InvokeSubAgent", arguments: new Dictionary<string, object?>
        {
            ["Prompt"] = "翻译"
        });
        var functionResult = new FunctionResultContent("sub-call-1", result: "已完成");

        message.AppendFunctionCall(functionCall);
        message.AppendFunctionResult(functionResult);

        Assert.HasCount(0, message.MessageItems, "InvokeSubAgent 结果不应在 MessageItems 中创建项");
    }

    [TestMethod]
    [Description("CreateSubAgentItem 仍应正常创建子代理项，由 SubAgentToolProvider 负责")]
    public void CreateSubAgentItem_WhenCalledBySubAgentToolProvider_CreatesSubAgentItem()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);

        CopilotChatSubAgentItem item = message.CreateSubAgentItem("调用子智能体", "翻译请求", "sub-call-1");

        Assert.IsNotNull(item);
        Assert.HasCount(1, message.MessageItems);
        Assert.AreSame(item, message.MessageItems[0]);
    }

    [TestMethod]
    [Description("先调用 AppendFunctionCall(InvokeSubAgent) 再调用 CreateSubAgentItem（模拟 SubAgentToolProvider）应只产生一个子代理项")]
    public void AppendFunctionCallAndCreateSubAgentItem_WhenInvokeSubAgent_ProducesExactlyOneSubAgentItem()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        var functionCall = new FunctionCallContent("sub-call-1", "InvokeSubAgent", arguments: new Dictionary<string, object?>
        {
            ["Prompt"] = "翻译"
        });

        message.AppendFunctionCall(functionCall);

        // SubAgentToolProvider 使用不同的 callId 创建子代理项
        CopilotChatSubAgentItem item = message.CreateSubAgentItem("调用子智能体", "翻译请求", "sub-agent-call-1");
        item.AppendText("处理中...");

        Assert.HasCount(1, message.MessageItems, "应只有一个子代理项");
        Assert.AreEqual("处理中...", item.MessageItems.OfType<CopilotChatTextItem>().First().Text);
    }

    [TestMethod]
    [Description("首次追加用量时，CurrentUsageDetails 应包含本次用量，TotalUsageDetails 也包含相同值")]
    public void AppendUsageDetails_FirstCall_SetsBothCurrentAndTotal()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "答案");
        var details = new UsageDetails
        {
            TotalTokenCount = 50,
            InputTokenCount = 20,
            OutputTokenCount = 30
        };

        message.AppendUsageDetails(details);

        Assert.IsNotNull(message.TotalUsageDetails);
        Assert.IsNotNull(message.CurrentUsageDetails);
        Assert.AreEqual(50, message.TotalUsageDetails!.TotalTokenCount);
        Assert.AreEqual(50, message.CurrentUsageDetails!.TotalTokenCount);
        Assert.AreEqual(20, message.CurrentUsageDetails.InputTokenCount);
        Assert.AreEqual(30, message.CurrentUsageDetails.OutputTokenCount);
    }

    [TestMethod]
    [Description("多次追加用量时，TotalUsageDetails 累加，CurrentUsageDetails 仅反映最新一次")]
    public void AppendUsageDetails_MultipleCalls_AccumulatesTotalAndReplacesCurrent()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "答案");
        var first = new UsageDetails
        {
            TotalTokenCount = 100,
            InputTokenCount = 60,
            OutputTokenCount = 40
        };
        var second = new UsageDetails
        {
            TotalTokenCount = 50,
            InputTokenCount = 30,
            OutputTokenCount = 20
        };

        message.AppendUsageDetails(first);
        message.AppendUsageDetails(second);

        Assert.IsNotNull(message.TotalUsageDetails);
        Assert.IsNotNull(message.CurrentUsageDetails);
        // Total 累加
        Assert.AreEqual(150, message.TotalUsageDetails!.TotalTokenCount);
        Assert.AreEqual(90, message.TotalUsageDetails.InputTokenCount);
        Assert.AreEqual(60, message.TotalUsageDetails.OutputTokenCount);
        // Current 只反映最新一次
        Assert.AreEqual(50, message.CurrentUsageDetails!.TotalTokenCount);
        Assert.AreEqual(30, message.CurrentUsageDetails.InputTokenCount);
        Assert.AreEqual(20, message.CurrentUsageDetails.OutputTokenCount);
    }

    [TestMethod]
    [Description("Clone 深拷贝时应同时复制 TotalUsageDetails 和 CurrentUsageDetails 引用")]
    public void Clone_WhenHasUsageDetails_CopiesTotalAndCurrentUsage()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "答案");
        var details = new UsageDetails
        {
            TotalTokenCount = 100,
            InputTokenCount = 50,
            OutputTokenCount = 50
        };
        message.AppendUsageDetails(details);

        CopilotChatMessage clone = message.Clone();

        Assert.IsNotNull(clone.TotalUsageDetails);
        Assert.IsNotNull(clone.CurrentUsageDetails);
        Assert.AreEqual(100, clone.TotalUsageDetails!.TotalTokenCount);
        Assert.AreEqual(100, clone.CurrentUsageDetails!.TotalTokenCount);
    }
}
