using AgentLib.Tools;

namespace AgentLib.Tests;

[TestClass]
public class WorkspaceToolProviderTests
{
    [TestMethod]
    [Description("主工作区为空时，读取文件应回退到副工作区")]
    public async Task ReadFile_WhenPrimaryWorkspaceIsEmpty_FallsBackToSecondaryWorkspace()
    {
        string testRoot = CreateTestDirectory();
        string secondaryWorkspacePath = Path.Combine(testRoot, "secondary");
        Directory.CreateDirectory(secondaryWorkspacePath);
        string filePath = Path.Combine(secondaryWorkspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "secondary-content");

        var provider = new WorkspaceToolProvider
        {
            SecondaryWorkspacePath = secondaryWorkspacePath
        };

        string result = await provider.ReadFile("note.txt");

        StringAssert.Contains(result, "secondary-content");
        StringAssert.Contains(result, "文件: note.txt");
    }

    [TestMethod]
    [Description("列目录只应使用主工作区，不应回退到副工作区")]
    public async Task ListDirectory_WhenPrimaryWorkspaceIsEmpty_DoesNotUseSecondaryWorkspace()
    {
        string testRoot = CreateTestDirectory();
        string secondaryWorkspacePath = Path.Combine(testRoot, "secondary");
        Directory.CreateDirectory(secondaryWorkspacePath);
        await File.WriteAllTextAsync(Path.Combine(secondaryWorkspacePath, "note.txt"), "secondary-content");

        var provider = new WorkspaceToolProvider
        {
            SecondaryWorkspacePath = secondaryWorkspacePath
        };

        string result = await provider.ListDirectory();

        Assert.AreEqual("当前未设置主工作路径，无法使用目录工具。", result);
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Combine(Path.GetTempPath(), "AgentLib.Tests", nameof(WorkspaceToolProviderTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}