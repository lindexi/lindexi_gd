using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class ToolDetailFormatterTests
{
    [TestMethod]
    public void FormatsNestedJsonAndDisplaysUnicodeCharacters()
    {
        const string input = "{\"content\":[{\"text\":\"\\u8bfe\\u4ef6\"}]}";

        var display = ToolDetailFormatter.Format(input);

        Assert.AreEqual("{\n  \"content\": [\n    {\n      \"text\": \"\u8bfe\u4ef6\"\n    }\n  ]\n}", display.ReplaceLineEndings("\n"));
    }

    [TestMethod]
    [DataRow("OK\nSecond line")]
    [DataRow("[File not found]")]
    [DataRow("{incomplete")]
    public void LeavesNonJsonTextUnchanged(string text)
    {
        Assert.AreEqual(text, ToolDetailFormatter.Format(text));
    }
}
