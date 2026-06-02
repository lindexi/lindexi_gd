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
        string secondaryWorkspacePath = Path.Join(testRoot, "secondary");
        Directory.CreateDirectory(secondaryWorkspacePath);
        string filePath = Path.Join(secondaryWorkspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "secondary-content");

        var provider = new WorkspaceToolProvider
        {
            SecondaryWorkspacePath = secondaryWorkspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 100);

        StringAssert.Contains(result, "secondary-content");
        StringAssert.Contains(result, "文件: note.txt");
        StringAssert.Contains(result, "<MetaData>");
        StringAssert.Contains(result, "</MetaData>");
    }

    [TestMethod]
    [Description("读取行内容时可关闭行号输出")]
    public async Task ReadFileLines_WhenIncludeLineNumbersIsFalse_DoesNotPrefixLineNumbers()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "first" + Environment.NewLine + "second");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 2, includeLineNumbers: false);

        StringAssert.Contains(result, $"行范围: 1-2{Environment.NewLine}</MetaData>{Environment.NewLine}first{Environment.NewLine}second");
        Assert.IsFalse(result.Contains("1: first", StringComparison.Ordinal));
    }

    [TestMethod]
    [Description("读取超过最大字符数时应返回实际读取范围并说明已截断")]
    public async Task ReadFileLines_WhenContentExceedsCharacterLimit_ReturnsActualRangeAndTruncationMessage()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        string lineContent = new('a', 1996);
        string content = string.Join(Environment.NewLine, Enumerable.Range(1, 3).Select(index => $"{lineContent}{index}"));
        await File.WriteAllTextAsync(filePath, content);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 3);

        StringAssert.Contains(result, "行范围: 1-3【超长截断】");
    }

    [TestMethod]
    [Description("首行超过最大字符数时应返回部分内容并保留实际读取行范围")]
    public async Task ReadFileLines_WhenFirstLineExceedsCharacterLimit_ReturnsPartialFirstLine()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, new string('a', 5000));

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 1, includeLineNumbers: false);

        StringAssert.Contains(result, "行范围: 1-1【超长截断】");
        StringAssert.Contains(result, new string('a', 100), StringComparison.Ordinal);
    }

    [TestMethod]
    [Description("读取范围超过文件末尾时应提示文件已读完")]
    public async Task ReadFileLines_WhenEndLineExceedsFileLength_ReportsEndOfFile()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "first" + Environment.NewLine + "second");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 5);

        StringAssert.Contains(result, "行范围: 1-2【已读完】");
    }

    [TestMethod]
    [Description("读取文件时不应去除末尾空白")]
    public async Task ReadFileLines_WhenLastLineContainsTrailingSpaces_PreservesTrailingSpaces()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "tail   ");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 1, includeLineNumbers: false);

        StringAssert.EndsWith(result, "tail   ");
    }

    [TestMethod]
    [Description("读取末行不应追加多余换行符")]
    public async Task ReadFileLines_WhenReadingLastLine_DoesNotAppendTrailingNewline()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "hello");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 1, includeLineNumbers: false);

        // 内容末尾不应该有换行符，即输出结尾就是 "hello" 而不是 "hello\r\n"
        Assert.IsTrue(result.EndsWith("hello", StringComparison.Ordinal));
    }

    [TestMethod]
    [Description("主工作区为空时，列目录会回退到副工作区")]
    public async Task ListDirectory_WhenPrimaryWorkspaceIsEmpty_FallsBackToSecondaryWorkspace()
    {
        string testRoot = CreateTestDirectory();
        string secondaryWorkspacePath = Path.Join(testRoot, "secondary");
        Directory.CreateDirectory(secondaryWorkspacePath);
        await File.WriteAllTextAsync(Path.Join(secondaryWorkspacePath, "note.txt"), "secondary-content");

        var provider = new WorkspaceToolProvider
        {
            SecondaryWorkspacePath = secondaryWorkspacePath
        };

        string result = await provider.ListDirectory();

        StringAssert.Contains(result, $"工作路径: {secondaryWorkspacePath}");
        StringAssert.Contains(result, "[文件] note.txt");
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Join(AppContext.BaseDirectory, "AgentLib.Tests", nameof(WorkspaceToolProviderTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}