using System.Text;
using Avalonia.Threading;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
public class SkiaTextEditorTest
{
    [TestMethod("Avalonia 文本框可以获取指导布局信息并覆盖横排对齐与竖排组合场景")]
    public async Task GuidingLayoutInfoTest()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            textEditor.Width = 120;
            textEditor.Height = 200;
            textEditor.SetFontName("Arial");
            textEditor.SetFontSize(30);
            textEditor.Text = "\nAB!?\n\n一二三四五六\n蒙古文";

            textEditor.TextEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(1), textEditor.TextEditorCore.DocumentManager.StyleParagraphProperty with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Center
            });
            textEditor.TextEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(3), textEditor.TextEditorCore.DocumentManager.StyleParagraphProperty with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Right
            });

            await textEditor.WaitForRenderCompletedAsync();

            var guidingLayoutInfo = textEditor.TextEditorCore.GetCurrentGuidingLayoutInfo();
            Assert.AreEqual(5, guidingLayoutInfo.ParagraphCount);
            Assert.IsTrue(guidingLayoutInfo.LineCount > guidingLayoutInfo.ParagraphCount);
            Assert.AreEqual(0, guidingLayoutInfo.ParagraphList[0].CharCount);
            Assert.IsTrue(guidingLayoutInfo.ParagraphList[3].LineCount > 1);
            Assert.IsTrue(guidingLayoutInfo.ParagraphList[1].LineList[0].ContentStartPoint.X > guidingLayoutInfo.ParagraphList[1].LineList[0].StartPoint.X);

            textEditor.ArrangingType = ArrangingType.Vertical;
            await textEditor.WaitForRenderCompletedAsync();

            var verticalGuidingLayoutInfo = textEditor.TextEditorCore.GetCurrentGuidingLayoutInfo();
            Assert.AreEqual(ArrangingType.Vertical, verticalGuidingLayoutInfo.ArrangingType);
            Assert.IsTrue(verticalGuidingLayoutInfo.LineCount >= verticalGuidingLayoutInfo.ParagraphCount);

            textEditor.ArrangingType = ArrangingType.Mongolian;
            await textEditor.WaitForRenderCompletedAsync();

            var mongolianGuidingLayoutInfo = textEditor.TextEditorCore.GetCurrentGuidingLayoutInfo();
            Assert.AreEqual(ArrangingType.Mongolian, mongolianGuidingLayoutInfo.ArrangingType);
            Assert.IsTrue(mongolianGuidingLayoutInfo.LineCount >= verticalGuidingLayoutInfo.ParagraphCount);
        });
    }

    [TestMethod("Avalonia 文本框设置指导布局后，可以按照指导布局执行渲染布局")]
    public async Task ApplyGuidingLayoutInfoTest()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            GuidingLayoutInfo guidingLayoutInfo;
            using (var sourceContext = TestFramework.CreateTextEditorInNewWindow())
            {
                var sourceTextEditor = sourceContext.TextEditor;
                sourceTextEditor.Width = 120;
                sourceTextEditor.SetFontName("Arial");
                sourceTextEditor.SetFontSize(20);
                sourceTextEditor.Text = "一二三四五六";
                await sourceTextEditor.WaitForRenderCompletedAsync();
                guidingLayoutInfo = sourceTextEditor.TextEditorCore.GetCurrentGuidingLayoutInfo();
            }

            using var targetContext = TestFramework.CreateTextEditorInNewWindow();
            var targetTextEditor = targetContext.TextEditor;
            targetTextEditor.Width = 120;
            targetTextEditor.SetFontName("Arial");
            targetTextEditor.SetFontSize(30);
            targetTextEditor.Text = "一二三四五六";
            await targetTextEditor.WaitForRenderCompletedAsync();

            targetTextEditor.TextEditorCore.SetGuidingLayoutInfoForNextUpdateLayout(guidingLayoutInfo);

            await targetTextEditor.WaitForRenderCompletedAsync();

            GuidingLayoutInfo appliedGuidingLayoutInfo = targetTextEditor.TextEditorCore.GetCurrentGuidingLayoutInfo();
            CollectionAssert.AreEqual(
                guidingLayoutInfo.ParagraphList.SelectMany(t => t.LineList).Select(t => t.CharCount).ToArray(),
                appliedGuidingLayoutInfo.ParagraphList.SelectMany(t => t.LineList).Select(t => t.CharCount).ToArray());
        });
    }

    [TestMethod("测试传入 emoji 表情和空格内容，预期可以正常布局")]
    [Ignore]
    public void MeasureEmojiCharData()
    {
        var skiaTextEditor = new SkiaTextEditor();
        var styleRunProperty = (SkiaTextRunProperty) skiaTextEditor.TextEditorCore.DocumentManager.StyleRunProperty;
        Rune emojiRune = new Rune(0x1F9EA); // 🧪
        skiaTextEditor.AppendRun(new SkiaTextRun($"{emojiRune} ", styleRunProperty with
        {
            FontName = new FontName("Segoe UI Emoji")
        }));
        var currentTextRender = skiaTextEditor.GetCurrentTextRender();
        currentTextRender.Dispose();
    }
}