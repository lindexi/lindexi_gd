using dotnetCampus.UITest.WPF;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document.Decorations;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests.Document.Decorations;

[TestClass()]
public class UnderlineTextEditorDecorationTest
{
    [UIContractTestCase]
    public void TestUnderlineTextEditorDecoration()
    {
        "三个字符，首个字符和其他字符的字号不相同，为这三个字符添加下划线，可以添加成功，下划线从最大字号字符下方开始画".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            textEditor.SetFontSize(60);
            // 先追加一段文本
            textEditor.AppendText("123");

            // 设置首个字符的字号为 90 让其与其他字符的字号不同
            textEditor.SetFontSize(90, new Selection(new CaretOffset(0), 1));

            // 添加下划线装饰
            textEditor.ToggleUnderline(textEditor.GetAllDocumentSelection());

            // 可以符合预期的显示下划线
            // 先靠人去看
            await TestFramework.FreezeTestToDebug();
        });
    }
}