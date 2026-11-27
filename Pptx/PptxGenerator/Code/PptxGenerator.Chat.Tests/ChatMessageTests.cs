using PptxGenerator.Chat.Services;
using PptxGenerator.Chat.ViewModels;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class ChatMessageTests
{
    [TestMethod]
    public void LateToolUpdateKeepsItsPlaceBetweenTextBlocks()
    {
        var message = new ChatMessage("Assistant", false, string.Empty, Array.Empty<ChatImage>());
        var call = new ToolActivity("Read", "read", "{}", "Pending", string.Empty);
        message.AppendText(0, "Before");
        message.AppendText(2, "After");

        message.UpdateTool(1, call);
        message.UpdateTool(1, call with { State = "Returned", Result = "Output" });
        message.AppendText(2, " tool");

        CollectionAssert.AreEqual(new[] { "Before", "Output", "After tool" }, message.Parts.Select(part => part switch
        {
            ChatTextPart text => text.Text,
            ChatToolPart tool => tool.Activity?.Result,
            _ => null,
        }).ToArray());
    }

    [TestMethod]
    public void CopyTextDoesNotIncludeToolDetails()
    {
        var message = new ChatMessage("Assistant", false, string.Empty, Array.Empty<ChatImage>());
        message.AppendText(0, "Before");
        message.UpdateTool(1, new ToolActivity("Read", "read", "{}", "Returned", "Output"));
        message.AppendText(2, "After");

        Assert.AreEqual("Before\n\nAfter", message.Text);
    }
}
