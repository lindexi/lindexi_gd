using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class ToolPresentationRegistrationTests
{
    [TestMethod]
    public void ReadFileLinesShouldShowFileAndLineRange()
    {
        ToolCallPresentation presentation = CreateWorkspacePresentation(
            "ReadFileLines",
            new Dictionary<string, object?>
            {
                ["filePath"] = "C:\\repo\\Views\\ChatView.axaml",
                ["startLine"] = 70,
                ["endLine"] = 150,
                ["includeLineNumbers"] = true,
            });

        Assert.AreEqual("Views\\ChatView.axaml", presentation.PrimaryText);
        Assert.AreEqual("第 70–150 行", presentation.SecondaryText);
    }

    [TestMethod]
    public void ReplaceStringInFileShouldOnlyShowFilePath()
    {
        ToolCallPresentation presentation = CreateWorkspacePresentation(
            "ReplaceStringInFile",
            new Dictionary<string, object?>
            {
                ["filePath"] = "ViewModels\\MessageItemViewModel.cs",
                ["oldString"] = "secret old text",
                ["newString"] = "secret new text",
            });

        Assert.AreEqual("ViewModels\\MessageItemViewModel.cs", presentation.PrimaryText);
        Assert.IsNull(presentation.SecondaryText);
    }

    [TestMethod]
    public void RunDotNetPublishShouldUsePascalCaseNameAndShowCommandLine()
    {
        ToolRegistration registration = new DotNetCliTools(Environment.CurrentDirectory)
            .AsToolRegistrations()
            .Single(item => item.Tool.Name == "RunDotNetPublish");
        var registry = new ToolRegistrationRegistry([registration]);
        const string commandLine = "dotnet publish Sample.csproj -c Release";
        var call = new FunctionCallContent("call-1", "RunDotNetPublish", new Dictionary<string, object?>
        {
            ["commandLine"] = commandLine,
        });

        ToolCallPresentation presentation = registry.CreatePresentation(call)!;

        Assert.AreEqual($"“{commandLine}”", presentation.PrimaryText);
    }

    [TestMethod]
    public void RunTestsShouldShowTargetProjectAndFilter()
    {
        ToolRegistration registration = new DotNetCliTools(Environment.CurrentDirectory)
            .AsToolRegistrations()
            .Single(item => item.Tool.Name == "run_tests");
        var registry = new ToolRegistrationRegistry([registration]);
        var call = new FunctionCallContent("call-1", "run_tests", new Dictionary<string, object?>
        {
            ["targetPath"] = "ChatRoom\\CodingChatRoom.AvaloniaShell.Tests.csproj",
            ["filter"] = "ChatViewModelTests",
        });

        ToolCallPresentation presentation = registry.CreatePresentation(call)!;

        Assert.AreEqual("ChatRoom\\CodingChatRoom.AvaloniaShell.Tests.csproj", presentation.PrimaryText);
        Assert.AreEqual("ChatViewModelTests", presentation.SecondaryText);
    }

    private static ToolCallPresentation CreateWorkspacePresentation(string name, Dictionary<string, object?> arguments)
    {
        var provider = new WorkspaceToolProvider { WorkspacePath = Environment.CurrentDirectory };
        ToolRegistration registration = provider.CreateDefaultToolRegistrations().Single(item => item.Tool.Name == name);
        return new ToolRegistrationRegistry([registration]).CreatePresentation(new FunctionCallContent("call-1", name, arguments))!;
    }
}
