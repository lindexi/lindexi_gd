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
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();

            textEditor.TextEditorCore.AppendText("123");

            await Task.Delay(TimeSpan.FromSeconds(1));

            mainWindow.Close();
        });
    }
}