using System.Windows;
using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorTest
{
    [UIContractTestCase]
    public void AppendTestAfterSetRunProperty()
    {
        "先追加一段文本，再修改当前光标属性，再追加一段文本，可以符合预期的显示两段样式不同的文本".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // 先追加一段文本
            textEditor.TextEditorCore.AppendText("123");

            // 再修改当前光标属性
            textEditor.SetRunProperty(runProperty => runProperty.FontSize = 15);

            // 再追加一段文本
            textEditor.TextEditorCore.AppendText("123");

            // 可以符合预期的显示两段样式不同的文本
            // 先靠人去看
            await TestFramework.FreezeTestToDebug();
        });
    }

    [UIContractTestCase]
    public void AppendText()
    {
        "给空的文本框追加 123 字符串，可以显示出 123 的文本".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            textEditor.TextEditorCore.AppendText("123");

            await TestFramework.FreezeTestToDebug();
        });
    }
}