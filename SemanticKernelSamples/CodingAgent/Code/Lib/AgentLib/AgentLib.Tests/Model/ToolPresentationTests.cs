using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Model;

[TestClass]
public sealed class ToolPresentationTests
{
    [TestMethod]
    public void AppendFunctionCallAndResultShouldPreservePresentation()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        var call = new FunctionCallContent("call-1", "ReadFileLines", new Dictionary<string, object?>());
        var presentation = new ToolCallPresentation(
            "Views\\ChatView.axaml",
            "第 70–150 行",
            "C:\\repo\\Views\\ChatView.axaml");

        message.AppendFunctionCall(call, presentation);
        CopilotChatToolItem item = message.MessageItems.OfType<CopilotChatToolItem>().Single();

        message.AppendFunctionResult(new FunctionResultContent("call-1", string.Empty));

        Assert.AreEqual("ReadFileLines", item.DisplayName);
    }

    [TestMethod]
    public void CloneShouldPreservePresentation()
    {
        var item = new CopilotChatToolItem(
            "call-1",
            "run_tests",
            "targetPath: Tests.csproj",
            "Passed",
            new ToolCallPresentation("Tests.csproj", "ChatViewModelTests", "C:\\repo\\Tests.csproj"));

        CopilotChatToolItem clone = (CopilotChatToolItem)((ICopilotChatMessageItem)item).Clone();

        Assert.AreEqual(
            (item.ToolName, item.PrimaryText, item.SecondaryText, item.FullTargetText, item.InputText, item.OutputText),
            (clone.ToolName, clone.PrimaryText, clone.SecondaryText, clone.FullTargetText, clone.InputText, clone.OutputText));
    }

    [TestMethod]
    public void RegistryShouldRejectDuplicateToolNames()
    {
        AITool first = AIFunctionFactory.Create(() => string.Empty, "duplicate");
        AITool second = AIFunctionFactory.Create(() => string.Empty, "duplicate");

        Assert.ThrowsExactly<InvalidOperationException>(() => new ToolRegistrationRegistry(
        [
            new(first),
            new(second),
        ]));
    }
}
