using System.Windows;
using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorStyleTest
{
    [UIContractTestCase]
    public void ToggleBold()
    {
        "未选择时，调用 ToggleBold 给文本当前光标设置加粗，追加文本之后，当前光标的字符属性是加粗".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // 这里获取到的是未选择的
            // 直接调用 ToggleBold 即可
            textEditor.ToggleBold();

            // 追加文本，预期追加文本不会导致当前光标的字符属性不加粗
            textEditor.TextEditorCore.AppendText("a");

            // Assert
            // 当前光标的字符属性是加粗
            Assert.AreEqual(FontWeights.Bold, textEditor.CurrentCaretRunProperty.FontWeight);
            await TestFramework.FreezeTestToDebug();
        });

        "未选择时，可以调用 ToggleBold 给文本当前光标设置加粗".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // 这里获取到的是未选择的
            // 直接调用 ToggleBold 即可
            textEditor.ToggleBold();

            // Assert
            Assert.AreEqual(FontWeights.Bold, textEditor.CurrentCaretRunProperty.FontWeight);

            // 重新调用 ToggleBold 可以去掉加粗
            textEditor.ToggleBold();
            Assert.AreEqual(FontWeights.Normal, textEditor.CurrentCaretRunProperty.FontWeight);

            await TestFramework.FreezeTestToDebug();
        });
    }
}