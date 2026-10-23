using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Model;

[TestClass]
public class CopilotChatSubAgentItemTests
{
    [TestMethod]
    [Description("子智能体追加文本与思考时应合并同类片段")]
    public void AppendTextAndReasoning_WhenCalledRepeatedly_MergesAdjacentItems()
    {
        var subAgentItem = new CopilotChatSubAgentItem("call-1", "子任务", "输入");

        subAgentItem.AppendText("第一段");
        subAgentItem.AppendText("第二段");
        subAgentItem.AppendReasoning("先想");
        subAgentItem.AppendReasoning("再想");

        Assert.HasCount(2, subAgentItem.MessageItems);
        Assert.IsTrue(subAgentItem.HasMessageItems);
    }

    [TestMethod]
    [Description("子智能体追加工具调用结果时应根据调用 Id 更新现有工具片段")]
    public void AppendFunctionCallAndResult_WhenToolCallIdMatches_UpdatesExistingToolItem()
    {
        var subAgentItem = new CopilotChatSubAgentItem("call-1", "子任务", "输入");
        var functionCall = new FunctionCallContent("tool-1", "ReadFile", arguments: new Dictionary<string, object?>
        {
            ["Path"] = "demo.txt"
        });
        var functionResult = new FunctionResultContent("tool-1", result: "完成");

        subAgentItem.AppendFunctionCall(functionCall);
        subAgentItem.AppendFunctionResult(functionResult);

        Assert.HasCount(1, subAgentItem.MessageItems);
        Assert.IsInstanceOfType<CopilotChatToolItem>(subAgentItem.MessageItems[0]);
        var toolItem = (CopilotChatToolItem)subAgentItem.MessageItems[0];
        Assert.AreEqual("ReadFile", toolItem.ToolName);
        StringAssert.Contains(toolItem.InputText, "demo.txt");
        Assert.AreEqual("完成", toolItem.OutputText);
    }

    [TestMethod]
    [Description("创建相同调用 Id 的子智能体片段时应复用现有实例并更新输入内容")]
    public void CreateSubAgentItem_WhenCallIdAlreadyExists_ReusesExistingItem()
    {
        var subAgentItem = new CopilotChatSubAgentItem("call-1", "父任务", "输入");

        CopilotChatSubAgentItem first = subAgentItem.CreateSubAgentItem("子任务", "第一次输入", "sub-1");
        CopilotChatSubAgentItem second = subAgentItem.CreateSubAgentItem("子任务", "第二次输入", "sub-1");

        Assert.AreSame(first, second);
        Assert.AreEqual("第二次输入", second.InputText);
        Assert.HasCount(1, subAgentItem.MessageItems);
    }
}
