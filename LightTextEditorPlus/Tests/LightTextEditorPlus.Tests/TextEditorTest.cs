using System.Windows;

using dotnetCampus.UITest.WPF;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorTest
{
    [UIContractTestCase]
    public void LayoutTest()
    {
        "设置 TextEditor 的宽度，可以影响到布局的行最大宽度".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // 设置 TextEditor 的宽度，设置为 50 宽度，字体选用微软雅黑 30 字体，那就放不下两个字符
            textEditor.Width = 50;
            textEditor.SetFontName("微软雅黑");
            textEditor.SetFontSize(30);

            // 再输入两个字符，预计就能被布局为两行
            textEditor.TextEditorCore.AppendText("一二");

            // Assert
            await textEditor.WaitForRenderCompletedAsync();

            await TestFramework.FreezeTestToDebug();
            var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
            // 布局渲染一段两行
            var paragraphRenderInfo = renderInfoProvider.GetParagraphRenderInfoList().First();
            var lineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();

            Assert.AreEqual(2, lineRenderInfoList.Count, "设置 TextEditor 的宽度，设置为 50 宽度，字体选用微软雅黑 30 字体，将布局两行");
        });
    }

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
            textEditor.SetRunProperty(runProperty => runProperty with
            {
                FontSize = 15
            });

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