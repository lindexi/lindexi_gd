using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorEditTest
{
    [ContractTestCase]
    public void Backspace()
    {
        "对只有一个字符的文本执行 Backspace 退格，可以删除所有文本".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加一个字符，用于后续删除
            textEditorCore.AppendText("1");

            // Action
            textEditorCore.Backspace();

            // Assert
            // 可以删除所有文本，等于文本字符数量是空
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);
            // 删除之后，依然存在一段
            Assert.AreEqual(1, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList().Count);
            Assert.AreEqual(1, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList()[0].LineLayoutDataList.Count);
        });
    }
}