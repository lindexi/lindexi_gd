using System.Windows;
using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorStyleTest
{
    [UIContractTestCase]
    public void ChangeStyle()
    {
        "未选择时，修改当前光标字符属性样式，只触发 StyleChanging 和 StyleChanged 事件，不触发布局变更".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            // 只触发 StyleChanging 和 StyleChanged 事件。不用测试了，交给其他单元测试
            textEditor.TextEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                // 未选择时，修改当前光标字符属性样式，只触发 StyleChanging 和 StyleChanged 事件，不触发布局变更
                Assert.Fail();
            };

            // Action
            textEditor.ToggleBold();
        });

        "修改样式时，先触发 StyleChanging 事件，再触发 StyleChanged 事件，且只触发一次".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            int count = 0;
            textEditor.StyleChanging += (sender, args) =>
            {
                Assert.AreEqual(0, count);
                count++;
            };
            textEditor.StyleChanged += (sender, args) =>
            {
                Assert.AreEqual(1, count);
                count++;
            };

            // Action
            textEditor.ToggleBold();

            // Assert
            // 只触发一次，一共两个事件
            Assert.AreEqual(2, count);
        });
    }

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