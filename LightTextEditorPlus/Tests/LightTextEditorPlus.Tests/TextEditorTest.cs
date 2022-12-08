using System.Windows;
using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorTest
{
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