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

        StringAssert.Contains(result, $"行范围: 1-2【已读完】{Environment.NewLine}</MetaData>{Environment.NewLine}first{Environment.NewLine}second");
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

    [TestMethod]
    [Description("读取部分行时应显示剩余行数")]
    public async Task ReadFileLines_WhenHasRemainingLines_ShowsRemainingLineCount()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        string content = string.Join(Environment.NewLine, Enumerable.Range(1, 10).Select(i => $"line{i}"));
        await File.WriteAllTextAsync(filePath, content);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 5, includeLineNumbers: false);

        // 读取 1-5 行后，剩余 6-10 行共 5 行
        StringAssert.Contains(result, "行范围: 1-5【剩余 5 行未读取】");
    }

    [TestMethod]
    [Description("读取部分行时，剩余行数刚好为0时应显示已读完")]
    public async Task ReadFileLines_WhenNoRemainingLines_ShowsEndOfFile()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        string content = string.Join(Environment.NewLine, Enumerable.Range(1, 5).Select(i => $"line{i}"));
        await File.WriteAllTextAsync(filePath, content);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 5, includeLineNumbers: false);

        Assert.IsFalse(result.Contains("剩余", StringComparison.Ordinal));
        StringAssert.Contains(result, "行范围: 1-5【已读完】");
    }

    [TestMethod]
    [Description("读取部分行时，剩余行数超过500行应显示大于500行提示")]
    public async Task ReadFileLines_WhenRemainingLinesExceed500_ShowsExceedsLimitMessage()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        string content = string.Join(Environment.NewLine, Enumerable.Range(1, 600).Select(i => $"line{i}"));
        await File.WriteAllTextAsync(filePath, content);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 50, includeLineNumbers: false);

        StringAssert.Contains(result, "行范围: 1-50【剩余大于 500 行未读取】");
    }

    [TestMethod]
    [Description("读取部分行时，剩余行数刚好为500行应显示具体行数")]
    public async Task ReadFileLines_WhenRemainingLinesExactly500_ShowsExactCount()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        // 读取 1-50 行，剩余 51-550 行共 500 行，总共 550 行
        string content = string.Join(Environment.NewLine, Enumerable.Range(1, 550).Select(i => $"line{i}"));
        await File.WriteAllTextAsync(filePath, content);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.ReadFileLines("note.txt", 1, 50, includeLineNumbers: false);

        StringAssert.Contains(result, "行范围: 1-50【剩余 500 行未读取】");
    }

    [TestMethod]
    [Description("FindFilesMatchingPattern 应返回文件名和截断后的匹配行上下文")]
    public async Task FindFilesMatchingPattern_WhenMatchFound_ReturnsFileNameAndTruncatedContext()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        string content = "line1 abc TheQueryText xyz end" + Environment.NewLine + "line2 no match here";
        await File.WriteAllTextAsync(filePath, content);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("TheQueryText");

        StringAssert.Contains(result, "note.txt:");
        StringAssert.Contains(result, "1: ");
        StringAssert.Contains(result, "TheQueryText");
        Assert.IsFalse(result.Contains("line2", StringComparison.Ordinal));
    }

    [TestMethod]
    [Description("查询字符串超过10字符时应在头部显示截断")]
    public async Task FindFilesMatchingPattern_WhenQueryExceeds10Chars_TruncatesQueryInHeader()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "ThisIsAVeryLongQueryTextForTesting");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("ThisIsAVeryLongQueryTextForTesting");

        // 超过 10 字符，头部模式行应截断为前5字符…后5字符
        StringAssert.Contains(result, "模式: ThisI…sting");
        // 头部模式行不应包含完整查询字符串
        string headerLine = result.Split(Environment.NewLine).First(l => l.StartsWith("模式:", StringComparison.Ordinal));
        Assert.IsFalse(headerLine.Contains("ThisIsAVeryLongQueryTextForTesting", StringComparison.Ordinal));
    }

    [TestMethod]
    [Description("查询字符串不超过10字符时不应截断")]
    public async Task FindFilesMatchingPattern_WhenQueryNotExceeding10Chars_DoesNotTruncateQuery()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "short");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("short");

        StringAssert.Contains(result, "模式: short");
    }

    [TestMethod]
    [Description("匹配在行首时，上下文前面不应有 …")]
    public async Task FindFilesMatchingPattern_WhenMatchAtLineStart_NoLeadingEllipsis()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "MatchAtStart followed by some more text to fill context");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("MatchAtStart");

        // 匹配在行首，前面不应有 …
        string[] lines = result.Split(Environment.NewLine);
        string hitLine = lines.First(l => l.StartsWith("1: ", StringComparison.Ordinal));
        Assert.IsFalse(hitLine.StartsWith("1: …", StringComparison.Ordinal));
    }

    [TestMethod]
    [Description("匹配在行尾时，上下文后面不应有 …")]
    public async Task FindFilesMatchingPattern_WhenMatchAtLineEnd_NoTrailingEllipsis()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "some text before MatchAtEnd");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("MatchAtEnd");

        // 匹配在行尾，后面不应有 …
        string[] lines = result.Split(Environment.NewLine);
        string hitLine = lines.First(l => l.StartsWith("1: ", StringComparison.Ordinal));
        Assert.IsFalse(hitLine.EndsWith("…", StringComparison.Ordinal));
    }

    [TestMethod]
    [Description("短行不应被截断")]
    public async Task FindFilesMatchingPattern_WhenLineIsShort_NoTruncation()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "ab xyz cd");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("xyz");

        // 短行不应该有 …
        string[] lines = result.Split(Environment.NewLine);
        string hitLine = lines.First(l => l.StartsWith("1: ", StringComparison.Ordinal));
        Assert.IsFalse(hitLine.Contains('…', StringComparison.Ordinal));
        StringAssert.Contains(hitLine, "ab xyz cd");
    }

    [TestMethod]
    [Description("长行中匹配应被截断，前后各有 …")]
    public async Task FindFilesMatchingPattern_WhenMatchInLongLine_TruncatesWithEllipsis()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        string prefix = new('a', 100);
        string suffix = new('b', 100);
        await File.WriteAllTextAsync(filePath, prefix + "TheMatch" + suffix);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("TheMatch");

        // 长行中匹配，前后应有 …
        string[] lines = result.Split(Environment.NewLine);
        string hitLine = lines.First(l => l.StartsWith("1: ", StringComparison.Ordinal));
        Assert.IsTrue(hitLine.StartsWith("1: …", StringComparison.Ordinal));
        Assert.IsTrue(hitLine.EndsWith("…", StringComparison.Ordinal));
        StringAssert.Contains(hitLine, "TheMatch");
    }

    [TestMethod]
    [Description("每文件命中行数超过限制时应截断并提示")]
    public async Task FindFilesMatchingPattern_WhenHitsExceedLimit_TruncatesAndShowsMessage()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        string content = string.Join(Environment.NewLine, Enumerable.Range(1, 30).Select(i => $"line{i} match here"));
        await File.WriteAllTextAsync(filePath, content);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern("match");

        StringAssert.Contains(result, "该文件命中行数过多，已截断");
        // 只显示 20 行（DefaultMaxLineHitsPerFile）
        int hitCount = result.Split(Environment.NewLine).Count(l => l.StartsWith("  ", StringComparison.Ordinal) && l.Contains("match"));
        Assert.AreEqual(0, hitCount); // 行以数字开头，不是空格
    }

    [TestMethod]
    [Description("正则表达式匹配应正确返回截断后的上下文")]
    public async Task FindFilesMatchingPattern_WithRegex_ReturnsTruncatedContext()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "prefix pattern123suffix more text");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = await provider.FindFilesMatchingPattern(@"pattern\d+", useRegex: true);

        StringAssert.Contains(result, "模式（正则）:");
        StringAssert.Contains(result, "pattern123");
    }

    [TestMethod]
    [Description("写入不存在的文件时应成功创建")]
    public void WriteFileContent_WhenFileDoesNotExist_CreatesFile()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "newfile.txt");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = provider.WriteFileContent("newfile.txt", "hello world");

        Assert.AreEqual("OK", result);
        Assert.AreEqual("hello world", File.ReadAllText(filePath));
    }

    [TestMethod]
    [Description("写入不存在的文件时应自动创建父目录")]
    public void WriteFileContent_WhenParentDirectoryNotExist_CreatesDirectoryAndFile()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = provider.WriteFileContent(Path.Join("sub", "newfile.txt"), "content");

        Assert.AreEqual("OK", result);
        string filePath = Path.Join(workspacePath, "sub", "newfile.txt");
        Assert.IsTrue(File.Exists(filePath));
        Assert.AreEqual("content", File.ReadAllText(filePath));
    }

    [TestMethod]
    [Description("先读取再写入已存在文件应成功")]
    public async Task WriteFileContent_AfterReading_OverwritesFile()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "original content");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        await provider.ReadFileLines("note.txt", 1, 1);
        string result = provider.WriteFileContent("note.txt", "updated content");

        Assert.AreEqual("OK", result);
        Assert.AreEqual("updated content", File.ReadAllText(filePath));
    }

    [TestMethod]
    [Description("未读取直接写入已存在文件应返回错误")]
    public async Task WriteFileContent_WithoutReadingFirst_ReturnsError()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "original content");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = provider.WriteFileContent("note.txt", "updated content");

        StringAssert.Contains(result, "未被读取过");
        Assert.AreNotEqual("OK", result);
        Assert.AreEqual("original content", File.ReadAllText(filePath));
    }

    [TestMethod]
    [Description("读取后文件被外部修改再写入应返回错误")]
    public async Task WriteFileContent_WhenFileModifiedExternally_ReturnsError()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "original content");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        await provider.ReadFileLines("note.txt", 1, 1);

        // 外部修改文件
        await Task.Delay(10);
        await File.WriteAllTextAsync(filePath, "externally modified content");

        string result = provider.WriteFileContent("note.txt", "updated content");

        StringAssert.Contains(result, "已被外部修改");
        Assert.AreNotEqual("OK", result);
        Assert.AreEqual("externally modified content", File.ReadAllText(filePath));
    }

    [TestMethod]
    [Description("写入路径超出工作区范围应返回错误")]
    public void WriteFileContent_WhenPathOutsideWorkspace_ReturnsError()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string outsidePath = Path.Join(testRoot, "outside", "note.txt");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        string result = provider.WriteFileContent(outsidePath, "content");

        StringAssert.Contains(result, "不在工作区范围内");
        Assert.AreNotEqual("OK", result);
    }

    [TestMethod]
    [Description("主工作区为空时，写入副工作区文件应成功")]
    public async Task WriteFileContent_WhenPrimaryWorkspaceEmpty_FallsBackToSecondary()
    {
        string testRoot = CreateTestDirectory();
        string secondaryWorkspacePath = Path.Join(testRoot, "secondary");
        Directory.CreateDirectory(secondaryWorkspacePath);
        string filePath = Path.Join(secondaryWorkspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "secondary content");

        var provider = new WorkspaceToolProvider
        {
            SecondaryWorkspacePath = secondaryWorkspacePath
        };

        await provider.ReadFileLines("note.txt", 1, 1);
        string result = provider.WriteFileContent("note.txt", "updated secondary");

        Assert.AreEqual("OK", result);
        Assert.AreEqual("updated secondary", File.ReadAllText(filePath));
    }

    [TestMethod]
    [Description("多次读取后，写入应以最近一次快照为准覆盖旧快照")]
    public async Task WriteFileContent_AfterMultipleReads_UsesLatestSnapshot()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        Directory.CreateDirectory(workspacePath);
        string filePath = Path.Join(workspacePath, "note.txt");
        await File.WriteAllTextAsync(filePath, "version1");

        var provider = new WorkspaceToolProvider
        {
            WorkspacePath = workspacePath
        };

        await provider.ReadFileLines("note.txt", 1, 1);

        // 外部修改后重新读取，更新快照
        await Task.Delay(10);
        await File.WriteAllTextAsync(filePath, "version2");
        await provider.ReadFileLines("note.txt", 1, 1);

        string result = provider.WriteFileContent("note.txt", "version3");

        Assert.AreEqual("OK", result);
        Assert.AreEqual("version3", File.ReadAllText(filePath));
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Join(AppContext.BaseDirectory, "AgentLib.Tests", nameof(WorkspaceToolProviderTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}