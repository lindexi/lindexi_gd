using LightTextEditorPlus.Core.Document;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class DefaultRunParagraphSplitterTest
{
    [ContractTestCase]
    public void Split()
    {
        "传入的文本的结尾包含连续两个换行符，可以多加两个空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("123\r\n\r\n");
            var result = splitter.Split(textRun).ToList();

            // Assert
            Assert.AreEqual(3, result.Count);
        });


        "传入的文本的结尾包含换行符，可以多加一个空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("123\r\n");
            var result = splitter.Split(textRun).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
        });

        "传入包含多个连续换行符的文本，可以分出空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("123\r\n\r\n\r\n123\r\n123");
            var result = splitter.Split(textRun).ToList();

            // Assert
            Assert.AreEqual(5, result.Count);
        });

        "传入包含多个换行符的文本，可以根据换行符进行分段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var textRun = new TextRun("123\r\n123\r\n123");
            var result = splitter.Split(textRun).ToList();

            // Assert
            Assert.AreEqual(3, result.Count);
        });

        "对于一段文本不包含任何换行符，可以分割之后，返回依然是一段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var splitter = textEditorCore.PlatformProvider.GetRunParagraphSplitter();

            // Action
            var result = splitter.Split(new TextRun("123")).ToList();

            // Assert
            Assert.AreEqual(1,result.Count);
        });
    }
}