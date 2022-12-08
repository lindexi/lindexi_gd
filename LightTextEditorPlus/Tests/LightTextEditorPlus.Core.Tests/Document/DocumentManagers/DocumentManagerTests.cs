using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

[TestClass()]
public class DocumentManagerTests
{
    [ContractTestCase]
    public void SetCurrentCaretRunProperty()
    {
        "对当前的光标设置文本字符属性，在光标移动之后，将会清空当前的设置".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // 先加一个字符，让光标可以移动
            textEditorCore.AppendText("1");

            // Action
            var fontSize = 0d;
            // 对当前的光标设置文本字符属性
            textEditorCore.DocumentManager.SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(runProperty =>
            {
                fontSize = runProperty.FontSize + 10;
                runProperty.FontSize = fontSize;
            });

            // 先证明已设置成功
            Assert.AreEqual(fontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);

            // 移动光标，期望将会清空当前的设置
            textEditorCore.CurrentCaretOffset = new CaretOffset(0);

            // Assert
            Assert.AreNotEqual(fontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);
        });

        "对当前的光标设置文本字符属性，可以获取当前的光标的字符属性获取到设置的属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            var fontSize = 0d;
            // 对当前的光标设置文本字符属性
            textEditorCore.DocumentManager.SetCurrentCaretRunProperty<LayoutOnlyRunProperty>(runProperty =>
            {
                fontSize = runProperty.FontSize + 10;
                runProperty.FontSize = fontSize;
            });

            // Assert
            // 可以获取当前的光标的字符属性获取到设置的属性
            Assert.AreEqual(fontSize, textEditorCore.DocumentManager.CurrentCaretRunProperty.FontSize);
        });
    }

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