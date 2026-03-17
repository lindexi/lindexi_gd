using Avalonia.Threading;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using SkiaSharp;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
public class TextEditorStyleTest
{
    [TestMethod("功能开关禁用之后，公开 API 不生效")]
    public async Task FeatureSwitchDisableShouldNotApply()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "abc";

            Selection selection = textEditor.GetAllDocumentSelection();
            SkiaTextRunProperty oldRunProperty = textEditor.GetRunPropertyRange(in selection).First();

            ParagraphProperty oldParagraphProperty = textEditor.GetCurrentCaretOffsetParagraphProperty();
            ParagraphProperty newParagraphProperty = oldParagraphProperty with { ParagraphBefore = oldParagraphProperty.ParagraphBefore + 10 };

            // Action
            textEditor.DisableFeatures(TextFeatures.SetFontSize | TextFeatures.SetParagraphSpaceBefore);
            textEditor.SetFontSize(200, selection);
            textEditor.SetCurrentCaretOffsetParagraphProperty(newParagraphProperty);

            // Assert
            SkiaTextRunProperty currentRunProperty = textEditor.GetRunPropertyRange(in selection).First();
            Assert.AreEqual(oldRunProperty.FontSize, currentRunProperty.FontSize);

            ParagraphProperty currentParagraphProperty = textEditor.GetCurrentCaretOffsetParagraphProperty();
            Assert.AreEqual(oldParagraphProperty, currentParagraphProperty);
        });
    }

    [TestMethod("样式功能开关逐项禁用验证")]
    public async Task FeatureSwitchStyleApisShouldNotApply()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.Text = "abc";
                Selection selection = textEditor.GetAllDocumentSelection();
                SkiaTextRunProperty oldRunProperty = textEditor.GetRunPropertyRange(in selection).First();
                textEditor.DisableFeatures(TextFeatures.SetFontName);

                textEditor.SetFontName("Times New Roman", selection);

                SkiaTextRunProperty currentRunProperty = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(oldRunProperty.FontName, currentRunProperty.FontName);
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.Text = "abc";
                Selection selection = textEditor.GetAllDocumentSelection();
                SkiaTextRunProperty oldRunProperty = textEditor.GetRunPropertyRange(in selection).First();
                textEditor.DisableFeatures(TextFeatures.SetForeground);

                textEditor.SetForeground(SKColors.Red, selection);

                SkiaTextRunProperty currentRunProperty = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(oldRunProperty.Foreground, currentRunProperty.Foreground);
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.DisableFeatures(TextFeatures.SetBold);

                textEditor.ToggleBold();

                Assert.AreEqual(SKFontStyleWeight.Normal, textEditor.CurrentCaretRunProperty.FontWeight);
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.DisableFeatures(TextFeatures.SetItalic);

                textEditor.ToggleItalic();

                Assert.AreEqual(SKFontStyleSlant.Upright, textEditor.CurrentCaretRunProperty.FontStyle);
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.DisableFeatures(TextFeatures.SetUnderline);

                textEditor.ToggleUnderline();

                Assert.IsFalse(textEditor.CurrentCaretRunProperty.DecorationCollection.Contains(UnderlineTextEditorDecoration.Instance));
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.DisableFeatures(TextFeatures.SetStriketh);

                textEditor.ToggleStrikethrough();

                Assert.IsFalse(textEditor.CurrentCaretRunProperty.DecorationCollection.Contains(StrikethroughTextEditorDecoration.Instance));
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.DisableFeatures(TextFeatures.SetFontSuperscript);

                textEditor.ToggleSuperscript();

                Assert.AreEqual(TextFontVariants.Normal, textEditor.CurrentCaretRunProperty.FontVariant.FontVariants);
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.DisableFeatures(TextFeatures.SetFontSubscript);

                textEditor.ToggleSubscript();

                Assert.AreEqual(TextFontVariants.Normal, textEditor.CurrentCaretRunProperty.FontVariant.FontVariants);
            }

            {
                using var context = TestFramework.CreateTextEditorInNewWindow();
                var textEditor = context.TextEditor;
                textEditor.Text = "abc";
                Selection selection = textEditor.GetAllDocumentSelection();
                textEditor.DisableFeatures(TextFeatures.SetFontSize);

                textEditor.SetRunProperty(runProperty => runProperty with { FontSize = 77 }, selection);

                SkiaTextRunProperty currentRunProperty = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(77, currentRunProperty.FontSize);
            }
        });
    }

    [TestMethod("IncreaseFontSize 和 DecreaseFontSize 应按选择范围增减字号")]
    public async Task IncreaseAndDecreaseFontSizeShouldApply()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "abc";

            Selection selection = textEditor.GetAllDocumentSelection();
            textEditor.SetFontSize(20, selection);

            textEditor.IncreaseFontSize(selection);
            SkiaTextRunProperty runPropertyAfterIncrease = textEditor.GetRunPropertyRange(in selection).First();
            Assert.AreEqual(21, runPropertyAfterIncrease.FontSize);

            textEditor.DecreaseFontSize(selection);
            SkiaTextRunProperty runPropertyAfterDecrease = textEditor.GetRunPropertyRange(in selection).First();
            Assert.AreEqual(20, runPropertyAfterDecrease.FontSize);
        });
    }

    [TestMethod("段落便捷 API 应支持当前光标、指定光标和指定段落设置")]
    public async Task ParagraphStyleApisShouldApply()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "aaa\nbbb";

            var secondParagraphCaretOffset = new CaretOffset("aaa\n".Length, isAtLineStart: true);
            var secondParagraphIndex = new ParagraphIndex(1);

            textEditor.SetIndentation(10);
            textEditor.IncreaseIndentation(secondParagraphCaretOffset, 4);
            textEditor.DecreaseIndentation(secondParagraphIndex, 1);
            textEditor.SetParagraphSpaceBefore(secondParagraphCaretOffset, 6);
            textEditor.SetParagraphSpaceAfter(secondParagraphIndex, 8);
            textEditor.SetLineSpacing(secondParagraphCaretOffset, TextLineSpacings.MultipleLineSpace(2));
            textEditor.SetHorizontalTextAlignment(HorizontalTextAlignment.Left);
            textEditor.SetHorizontalTextAlignment(secondParagraphCaretOffset, HorizontalTextAlignment.Center);
            textEditor.SetHorizontalTextAlignment(secondParagraphIndex, HorizontalTextAlignment.Justify);

            ParagraphProperty firstParagraphProperty = textEditor.GetParagraphProperty(new ParagraphIndex(0));
            ParagraphProperty secondParagraphProperty = textEditor.GetParagraphProperty(secondParagraphIndex);

            Assert.AreEqual(10, firstParagraphProperty.Indent);
            Assert.AreEqual(3, secondParagraphProperty.Indent);
            Assert.AreEqual(6, secondParagraphProperty.ParagraphBefore);
            Assert.AreEqual(8, secondParagraphProperty.ParagraphAfter);
            Assert.AreEqual(TextLineSpacings.MultipleLineSpace(2), secondParagraphProperty.LineSpacing);
            Assert.AreEqual(HorizontalTextAlignment.Left, firstParagraphProperty.HorizontalTextAlignment);
            Assert.AreEqual(HorizontalTextAlignment.Justify, secondParagraphProperty.HorizontalTextAlignment);
        });
    }

    [TestMethod("禁用段落功能开关后，新增段落便捷 API 不生效")]
    public async Task ParagraphStyleApisShouldRespectFeatureSwitch()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            textEditor.Text = "aaa\nbbb";

            var secondParagraphCaretOffset = new CaretOffset("aaa\n".Length, isAtLineStart: true);
            var secondParagraphIndex = new ParagraphIndex(1);
            ParagraphProperty oldSecondParagraphProperty = textEditor.GetParagraphProperty(secondParagraphIndex);

            textEditor.DisableFeatures(TextFeatures.SetIndentation | TextFeatures.IncreaseIndentation | TextFeatures.DecreaseIndentation
                | TextFeatures.SetParagraphSpaceBefore | TextFeatures.SetParagraphSpaceAfter | TextFeatures.SetLineSpacing
                | TextFeatures.AlignHorizontalRight);

            textEditor.SetIndentation(9);
            textEditor.IncreaseIndentation(secondParagraphCaretOffset, 2);
            textEditor.DecreaseIndentation(secondParagraphIndex, 2);
            textEditor.SetParagraphSpaceBefore(secondParagraphCaretOffset, 6);
            textEditor.SetParagraphSpaceAfter(secondParagraphIndex, 7);
            textEditor.SetLineSpacing(secondParagraphCaretOffset, TextLineSpacings.MultipleLineSpace(3));
            textEditor.SetHorizontalTextAlignment(secondParagraphIndex, HorizontalTextAlignment.Right);

            ParagraphProperty currentSecondParagraphProperty = textEditor.GetParagraphProperty(secondParagraphIndex);
            Assert.AreEqual(oldSecondParagraphProperty, currentSecondParagraphProperty);
        });
    }

    [TestMethod("整段文本字符属性设置之后，经过撤销恢复，能够还原状态")]
    public async Task TestSetRunProperty1()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            SkiaTextRunProperty runProperty = textEditor.CreateRunProperty(property => property with
            {
                FontSize = 62.5,
                FontWeight = SKFontStyleWeight.Bold,
            });
            textEditor.Text = "123\nabc123";

            await textEditor.WaitForRenderCompletedAsync();

            textEditor.CurrentCaretOffset = new CaretOffset("123\n".Length, isAtLineStart: true);
            ITextParagraph paragraph = textEditor.GetCurrentCaretOffsetParagraph();

            var selection = textEditor.GetParagraphSelection(paragraph);
            var originRunProperty = textEditor.GetRunPropertyRange(in selection).First();

            // Action
            textEditor.SetRunProperty(runProperty, selection);

            // Assert
            var runProperty1 = textEditor.GetRunPropertyRange(in selection).First();
            Assert.AreEqual(runProperty, runProperty1, "设置进去的字符属性，应该能够设置成功，能够拿到传入的字符属性");
            // 预期此时就和原始的不相同的了
            Assert.AreNotEqual(originRunProperty, runProperty1);
            // 原始的应该和样式相同
            Assert.AreEqual(textEditor.StyleRunProperty, originRunProperty);

            // 再测试撤销恢复
            for (int i = 0; i < 10; i++)
            {
                // Action
                // 撤销之后，应该和原始的相同
                textEditor.TextEditorCore.UndoRedoProvider.Undo();
                // Assert
                var runProperty2 = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(originRunProperty, runProperty2, "撤销之后，应该能还原为和原来的相同的文本字符属性");

                if (i > 5)
                {
                    // 同时也要测试经过了 UI 布局渲染之后的情况
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }

                // Action
                textEditor.TextEditorCore.UndoRedoProvider.Redo();
                // Assert
                var runProperty3 = textEditor.GetRunPropertyRange(in selection).First();
                Assert.AreEqual(runProperty, runProperty3);

                if (i > 5)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }
            }

            await TestFramework.FreezeTestToDebug();
        });
    }
}