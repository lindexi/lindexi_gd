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
    [Description("追加子智能体调用与结果时应生成包含进度与输出的子代理片段")]
    public void AppendFunctionCallAndResult_WhenUsingSubAgent_CreatesSubAgentItem()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        var functionCall = new FunctionCallContent("sub-1", "InvokeSubAgent", arguments: new Dictionary<string, object?>
        {
            ["Task"] = "翻译"
        });
        var functionResult = new FunctionResultContent("sub-1", result: "已完成");

        message.AppendFunctionCall(functionCall);
        CopilotChatSubAgentItem subAgentItem = message.CreateSubAgentItem("InvokeSubAgent", "翻译", "sub-1");
        subAgentItem.AppendText("处理中");
        message.AppendFunctionResult(functionResult);

        Assert.HasCount(1, message.MessageItems);
        StringAssert.Contains(message.FullContent, "子代理：InvokeSubAgent");
        StringAssert.Contains(message.FullContent, "进度：");
        StringAssert.Contains(message.FullContent, "处理中");
        StringAssert.Contains(message.FullContent, "输出：");
        StringAssert.Contains(message.FullContent, "已完成");
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
}
