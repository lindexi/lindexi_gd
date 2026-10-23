using AgentLib.Logging;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using System.Xml.Linq;

namespace AgentLib.Tests.Model;

[TestClass]
public sealed class CopilotChatToolProcessTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void WhenCallStreamsAgainThenSameToolAndDetailsAreRetained(bool nested)
    {
        ICopilotChatOutput output = nested
            ? new CopilotChatSubAgentItem("outer", "history", "task")
            : CopilotChatMessage.CreateAssistant(string.Empty, false);
        var call = new FunctionCallContent("call", "read", new Dictionary<string, object?> { ["path"] = "first" });
        CopilotChatToolItem item = output.CreateToolItem(call);
        var details = CopilotChatMessage.CreateAssistant(string.Empty, false);
        item.Details = details;
        output.AppendFunctionCall(new FunctionCallContent("call", "read", new Dictionary<string, object?> { ["path"] = "updated" }));
        Assert.AreSame(details, output.CreateToolItem(call).Details);
    }

    [TestMethod]
    public void WhenCallUpdatesThenInputIsUpdated()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        CopilotChatToolItem item = message.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>()));
        message.AppendFunctionCall(new FunctionCallContent("call", "read", new Dictionary<string, object?> { ["path"] = "updated" }));
        StringAssert.Contains(item.InputText, "updated");
    }

    [TestMethod]
    public void WhenToolIsOrdinaryThenDetailsAreAbsent()
    {
        var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
        Assert.IsNull(message.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>())).Details);
    }

    [TestMethod]
    public void WhenDetailsChangeThenParentReceivesNotification()
    {
        var parent = CopilotChatMessage.CreateAssistant(string.Empty, false);
        CopilotChatToolItem item = parent.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>()));
        item.Details = CopilotChatMessage.CreateAssistant(string.Empty, false);
        var properties = new List<string?>();
        parent.PropertyChanged += (_, args) => properties.Add(args.PropertyName);
        item.Details.AppendText("detail text");
        CollectionAssert.Contains(properties, nameof(CopilotChatMessage.FullContent));
    }

    [TestMethod]
    public void WhenDetailsChangeThenCopyIncludesDetails()
    {
        var parent = CopilotChatMessage.CreateAssistant(string.Empty, false);
        CopilotChatToolItem item = parent.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>()));
        item.Details = CopilotChatMessage.CreateAssistant("detail text", false);
        StringAssert.Contains(parent.FullContent, "detail text");
    }

    [TestMethod]
    public void WhenDetailsAreReplacedThenOldMessageNoLongerNotifiesTool()
    {
        var item = new CopilotChatToolItem("call", "read", "input");
        var oldDetails = CopilotChatMessage.CreateAssistant(string.Empty, false);
        item.Details = oldDetails;
        item.Details = CopilotChatMessage.CreateAssistant(string.Empty, false);
        int changes = 0;
        item.PropertyChanged += (_, _) => changes++;
        oldDetails.AppendText("old");
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public void WhenDetailsAreClonedThenTextAndUsageAreIndependent()
    {
        var parent = CopilotChatMessage.CreateAssistant(string.Empty, false);
        CopilotChatToolItem item = parent.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>()));
        item.Details = CopilotChatMessage.CreateAssistant("detail", false);
        item.Details.AppendUsageDetails(new UsageDetails { InputTokenCount = 7 });
        CopilotChatToolItem clone = parent.Clone().MessageItems.OfType<CopilotChatToolItem>().Single();
        clone.Details!.AppendText(" changed");
        clone.Details.AppendUsageDetails(new UsageDetails { InputTokenCount = 3 });
        Assert.AreEqual(("detail", 7L), (item.Details.Content, item.Details.TotalUsageDetails?.InputTokenCount));
    }

    [TestMethod]
    public void WhenXmlRoundTripsThenStructuredDetailsAndCallReuseAreRestored()
    {
        var parent = CopilotChatMessage.CreateAssistant(string.Empty, false);
        CopilotChatToolItem item = parent.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>()));
        item.Details = CopilotChatMessage.CreateAssistant("detail", false);
        item.Details.AppendReasoning("reasoning");
        item.Details.AppendFunctionCall(new FunctionCallContent("inner", "inspect", new Dictionary<string, object?>()));
        item.Details.AppendFunctionResult(new FunctionResultContent("inner", "inspection"));
        parent.AppendFunctionResult(new FunctionResultContent("call", "returned"));
        var document = new XDocument(CopilotChatHistoryXmlCodec.CreateRootElement(Guid.NewGuid(), DateTimeOffset.Now,
            "history", 2, [parent], null));
        CopilotChatMessage restored = CopilotChatHistoryXmlCodec.ReadPersistenceData(document).Messages.Single();
        CopilotChatToolItem restoredItem = restored.MessageItems.OfType<CopilotChatToolItem>().Single();
        restored.AppendFunctionCall(new FunctionCallContent("call", "read", new Dictionary<string, object?>()));
        Assert.AreEqual((1, "detail", "reasoning", "inspection", "returned"),
            (restored.MessageItems.Count, restoredItem.Details!.Content, restoredItem.Details.Reason,
                restoredItem.Details.MessageItems.OfType<CopilotChatToolItem>().Single().OutputText, restoredItem.OutputText));
    }

    [TestMethod]
    public void WhenDetailsExistThenModelMessageDoesNotContainDetails()
    {
        var parent = CopilotChatMessage.CreateAssistant(string.Empty, false);
        parent.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>())).Details
            = CopilotChatMessage.CreateAssistant("private detail", false);
        Assert.IsEmpty(parent.ToChatMessage().Contents);
    }

    [TestMethod]
    public void WhenOldXmlHasNoDetailsThenOrdinaryToolIsReadable()
    {
        var parent = CopilotChatMessage.CreateAssistant(string.Empty, false);
        parent.CreateToolItem(new FunctionCallContent("call", "read", new Dictionary<string, object?>()));
        var document = new XDocument(CopilotChatHistoryXmlCodec.CreateRootElement(Guid.NewGuid(), DateTimeOffset.Now,
            "history", 2, [parent], null));
        Assert.IsNull(CopilotChatHistoryXmlCodec.ReadPersistenceData(document).Messages.Single()
            .MessageItems.OfType<CopilotChatToolItem>().Single().Details);
    }
}
