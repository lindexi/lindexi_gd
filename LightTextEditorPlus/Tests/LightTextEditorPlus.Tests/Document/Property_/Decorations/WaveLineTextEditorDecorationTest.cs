using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document.Decorations;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests.Document.Decorations;

[TestClass()]
public class WaveLineTextEditorDecorationTest
{
    [UIContractTestCase]
    public void TestWaveLineTextEditorDecoration()
    {
        "三个字符，首个字符和其他字符的字号不相同，为这三个字符添加波浪线，可以添加成功".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            textEditor.SetFontSize(60);
            // 先追加一段文本
            textEditor.TextEditorCore.AppendText("123");

            // 设置首个字符的字号为 90 让其与其他字符的字号不同
            textEditor.SetFontSize(90, new Selection(new CaretOffset(0), 1));

            // 添加波浪线装饰
            textEditor.AddTextDecoration(new WaveLineTextEditorDecoration(), new Selection(new CaretOffset(0), 3));

            // 可以符合预期的显示波浪线
            // 先靠人去看
            await TestFramework.FreezeTestToDebug();
        });
    }
}
