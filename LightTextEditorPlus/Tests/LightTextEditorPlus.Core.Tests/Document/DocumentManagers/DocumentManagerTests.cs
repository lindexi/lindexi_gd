using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

[TestClass()]
public class DocumentManagerTests
{
    [ContractTestCase]
    public void GetCharCount()
    {
        "插入两段文本，获取文档字符数量，将会加上换行符".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.AppendText("1\r\n23");

            // Assert
            Assert.AreEqual(5, textEditorCore.DocumentManager.CharCount);
        });

        "插入一行123纯文本，获取文档字符数量，可以获取到3个".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.AppendText("123");

            // Assert
            Assert.AreEqual(3, textEditorCore.DocumentManager.CharCount);
        });
    }
}