using AgentLib.Tools;

namespace AgentLib.Tests;

[TestClass]
public class WorkspaceFileStringReplacerTests
{
    [TestMethod]
    [Description("替换唯一匹配的文本应成功")]
    public void ReplaceInContent_WhenOldStringIsUnique_ReturnsSuccess()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "line1\nline2\nline3";

        var outcome = replacer.ReplaceInContent(content, "line2", "replaced", "test.txt");

        Assert.IsTrue(outcome.Success);
        Assert.AreEqual("OK", outcome.Message);
        Assert.AreEqual("line1\nreplaced\nline3", outcome.NewContent);
    }

    [TestMethod]
    [Description("oldString 未找到时应返回失败")]
    public void ReplaceInContent_WhenOldStringNotFound_ReturnsFailure()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "line1\nline2\nline3";

        var outcome = replacer.ReplaceInContent(content, "notfound", "replaced", "test.txt");

        Assert.IsFalse(outcome.Success);
        StringAssert.Contains(outcome.Message, "未找到要替换的文本");
        StringAssert.Contains(outcome.Message, "test.txt");
        Assert.IsNull(outcome.NewContent);
    }

    [TestMethod]
    [Description("oldString 匹配多处时应返回失败")]
    public void ReplaceInContent_WhenOldStringMatchesMultipleTimes_ReturnsFailure()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "line1\nline2\nline2\nline3";

        var outcome = replacer.ReplaceInContent(content, "line2", "replaced", "test.txt");

        Assert.IsFalse(outcome.Success);
        StringAssert.Contains(outcome.Message, "找到 2 处匹配");
        StringAssert.Contains(outcome.Message, "唯一匹配");
        Assert.IsNull(outcome.NewContent);
    }

    [TestMethod]
    [Description("替换空字符串应成功（插入操作）")]
    public void ReplaceInContent_WhenNewStringIsEmpty_ReturnsSuccess()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "line1\nline2\nline3";

        var outcome = replacer.ReplaceInContent(content, "line2", "", "test.txt");

        Assert.IsTrue(outcome.Success);
        Assert.AreEqual("line1\n\nline3", outcome.NewContent);
    }

    [TestMethod]
    [Description("替换包含特殊字符的文本应成功")]
    public void ReplaceInContent_WhenOldStringContainsSpecialChars_ReturnsSuccess()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "public void Test() {\n    Console.WriteLine(\"Hello\");\n}";

        var outcome = replacer.ReplaceInContent(
            content, 
            "Console.WriteLine(\"Hello\");", 
            "Console.WriteLine(\"World\");", 
            "test.cs");

        Assert.IsTrue(outcome.Success);
        Assert.AreEqual("public void Test() {\n    Console.WriteLine(\"World\");\n}", outcome.NewContent);
    }

    [TestMethod]
    [Description("替换包含换行符的文本应成功")]
    public void ReplaceInContent_WhenOldStringContainsNewlines_ReturnsSuccess()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "line1\nline2\nline3\nline4";

        var outcome = replacer.ReplaceInContent(content, "line2\nline3", "replaced", "test.txt");

        Assert.IsTrue(outcome.Success);
        Assert.AreEqual("line1\nreplaced\nline4", outcome.NewContent);
    }

    [TestMethod]
    [Description("替换整个文件内容应成功")]
    public void ReplaceInContent_WhenOldStringIsEntireContent_ReturnsSuccess()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "entire content";

        var outcome = replacer.ReplaceInContent(content, "entire content", "new content", "test.txt");

        Assert.IsTrue(outcome.Success);
        Assert.AreEqual("new content", outcome.NewContent);
    }

    [TestMethod]
    [Description("区分大小写匹配")]
    public void ReplaceInContent_WhenCaseDiffers_ReturnsFailure()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "Line1\nLine2\nLine3";

        var outcome = replacer.ReplaceInContent(content, "line2", "replaced", "test.txt");

        Assert.IsFalse(outcome.Success);
        StringAssert.Contains(outcome.Message, "未找到要替换的文本");
    }

    [TestMethod]
    [Description("匹配三处时应返回失败并显示匹配数")]
    public void ReplaceInContent_WhenOldStringMatchesThreeTimes_ReturnsFailureWithCount()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "match\nmatch\nmatch\nother";

        var outcome = replacer.ReplaceInContent(content, "match", "replaced", "test.txt");

        Assert.IsFalse(outcome.Success);
        StringAssert.Contains(outcome.Message, "找到 3 处匹配");
    }

    [TestMethod]
    [Description("替换后内容应保留原始换行符")]
    public void ReplaceInContent_WhenContentHasMixedNewlines_PreservesNewlines()
    {
        var replacer = new WorkspaceFileStringReplacer();
        string content = "line1\r\nline2\r\nline3";

        var outcome = replacer.ReplaceInContent(content, "line2", "replaced", "test.txt");

        Assert.IsTrue(outcome.Success);
        Assert.AreEqual("line1\r\nreplaced\r\nline3", outcome.NewContent);
    }
}
