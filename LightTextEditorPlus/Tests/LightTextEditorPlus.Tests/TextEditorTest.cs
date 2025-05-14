using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using CSharpMarkup.Wpf;

using dotnetCampus.UITest.WPF;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Demo;

using MSTest.Extensions.Contracts;

using static CSharpMarkup.Wpf.Helpers;

using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorTest
{
    [UIContractTestCase]
    public void MeasureTest()
    {
        "将高度自适应的文本放入到水平布局的 StackPanel 容器里，创建文本时立刻设置文本控件 Width 宽度，文本布局之后能够应用设置的 Width 宽度进行高度自适应布局".Test(async () =>
        {
            // Arrange
            // Action
            const double width = 30;
            TextEditor textEditor = new TextEditor()
            {
                // 设置文本控件 Width 宽度
                Width = width,
                Margin = Thickness(10, 10, 10, 10),
                Text = "1234567890",
                SizeToContent = SizeToContent.Height
            };

            textEditor.SetFontName("微软雅黑");
            textEditor.SetFontSize(30);

            var mainWindow = new Window()
            {
                Title = "文本库 UI 单元测试",
                Width = 1000,
                Height = 700,
                Content = Border
                (
                    BorderThickness: Thickness(1),
                    BorderBrush: Brushes.Blue,
                    Child: StackPanel
                    (
                        textEditor
                    ).Orientation(Orientation.Horizontal)
                ).UI
            };

            using var context = new TextEditTestContext(mainWindow, textEditor);

            // Assert
            mainWindow.Show();
            await textEditor.WaitForRenderCompletedAsync();

            Assert.AreEqual(width + textEditor.Margin.Left + textEditor.Margin.Right, textEditor.DesiredSize.Width);
            Assert.AreEqual(width, textEditor.ActualWidth);
            RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
            Assert.AreEqual(width, renderInfoProvider.GetDocumentLayoutBounds().DocumentOutlineBounds.Width);

            await TestFramework.FreezeTestToDebug();
        });
    }

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
    public void AppendTextAfterSetRunProperty()
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
        "追加 Emoji 表情字符，可以显示出表情字符".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            textEditor.TextEditorCore.AppendText("😊123");

            await TestFramework.FreezeTestToDebug();
        });

        "给空的文本框追加 123 字符串，可以显示出 123 的文本".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            textEditor.TextEditorCore.AppendText("123");

            await TestFramework.FreezeTestToDebug();
        });
    }
}