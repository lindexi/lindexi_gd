using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Model;

[TestClass]
public sealed class CopilotChatOutputTests
{
    [TestMethod]
    public void AppendTextWhenUsingOutputInterfaceShouldUpdateAssistantMessage()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false);
        ICopilotChatOutput output = message;

        output.AppendText("done");

        Assert.AreEqual("done", message.Content);
    }

    [TestMethod]
    public void AppendReasoningWhenUsingOutputInterfaceShouldUpdateAssistantMessage()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false);
        ICopilotChatOutput output = message;

        output.AppendReasoning("reason");

        Assert.AreEqual("reason", message.Reason);
    }

    [TestMethod]
    public void ClearPlaceholderWhenMessageHasPlaceholderShouldClearContent()
    {
        var message = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, isPresetInfo: false);
        ICopilotChatOutput output = message;

        output.ClearPlaceholder();

        Assert.AreEqual(string.Empty, message.Content);
    }

    [TestMethod]
    public void ClearPlaceholderWhenMessageHasOutputShouldPreserveContent()
    {
        var message = CopilotChatMessage.CreateAssistant("done", isPresetInfo: false);
        ICopilotChatOutput output = message;

        output.ClearPlaceholder();

        Assert.AreEqual("done", message.Content);
    }

    [TestMethod]
    public void AppendFunctionCallWhenUsingOutputInterfaceShouldPreservePresentation()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false);
        ICopilotChatOutput output = message;
        var presentation = new ToolCallPresentation("target", "detail");

        output.AppendFunctionCall(new FunctionCallContent("call", "ReadFile", new Dictionary<string, object?>()), presentation);

        Assert.AreEqual("target", ((CopilotChatToolItem)message.MessageItems[0]).PrimaryText);
    }

    [TestMethod]
    public void AppendFunctionResultWhenUsingOutputInterfaceShouldUpdateToolOutput()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false);
        ICopilotChatOutput output = message;
        output.AppendFunctionCall(new FunctionCallContent("call", "ReadFile", new Dictionary<string, object?>()));

        output.AppendFunctionResult(new FunctionResultContent("call", "done"));

        Assert.AreEqual("done", ((CopilotChatToolItem)message.MessageItems[0]).OutputText);
    }

    [TestMethod]
    public void AppendUsageDetailsWhenUsingOutputInterfaceShouldAccumulateUsage()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false);
        ICopilotChatOutput output = message;

        output.AppendUsageDetails(new UsageDetails { TotalTokenCount = 3 });
        output.AppendUsageDetails([new UsageContent(new UsageDetails { TotalTokenCount = 5 })]);

        Assert.AreEqual(8L, message.TotalUsageDetails?.TotalTokenCount);
    }

    [TestMethod]
    public void AppendUsageDetailsWhenUsingSubAgentOutputInterfaceShouldAccumulateUsage()
    {
        var item = new CopilotChatSubAgentItem("child", "child", "task");
        ICopilotChatOutput output = item;

        output.AppendUsageDetails(new UsageDetails { TotalTokenCount = 3 });
        output.AppendUsageDetails([new UsageContent(new UsageDetails { TotalTokenCount = 5 })]);

        Assert.AreEqual(8L, item.TotalUsageDetails?.TotalTokenCount);
    }

    [TestMethod]
    public void AppendUsageDetailsWhenUsingSubAgentOutputInterfaceShouldReplaceCurrentUsage()
    {
        var item = new CopilotChatSubAgentItem("child", "child", "task");
        ICopilotChatOutput output = item;

        output.AppendUsageDetails(new UsageDetails { TotalTokenCount = 3 });
        output.AppendUsageDetails(new UsageDetails { TotalTokenCount = 5 });

        Assert.AreEqual(5L, item.CurrentUsageDetails?.TotalTokenCount);
    }
}
