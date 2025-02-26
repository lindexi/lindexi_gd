using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorStatusTest
{
    /// <summary>
    /// 测试命中测试，命中到段落之后的空白
    /// </summary>
    [ContractTestCase]
    public void TestTryHitTest_HitParagraphAfter()
    {
        "对包含两段的文本进行命中测试，文本首段和第二段存在第二段的段前间距，命中到首段的段前间距，可以返回命中到第二段".Test(() =>
        {
            // Arrange
            const double fontSize = 20;
            const double paragraphBefore = 10;
            var textEditorCore = TestHelper.GetLayoutTestTextEditor(lineCharCount: 5, fontSize: fontSize);
            // 文本首段和第二段存在第二段的段前间距
            textEditorCore.DocumentManager.SetStyleParagraphProperty(
                textEditorCore.DocumentManager.StyleParagraphProperty with
                {
                    ParagraphBefore = paragraphBefore
                });
            // 再添加两段用来测试。一行能布局 5 个字符，特意写第二段包含 6 个字符，确保命中到第二段的首行
            textEditorCore.AppendText("123\nabcabc");
            // Action
            // 第一段的 Outline 高度为 fontSize = 20，于是取 fontSize + 第二段.ParagraphBefore / 2 = 25 确保命中到第二段的段前间距的空白地方
            var point = new TextPoint(10, fontSize + paragraphBefore / 2);
            bool result = textEditorCore.TryHitTest(point, out var hitTestResult);

            // Assert
            Assert.AreEqual(true, result);
            Assert.AreEqual(1, hitTestResult.HitParagraphIndex.Index, "可以命中到第二段首行");
            Assert.IsNotNull(hitTestResult.LineLayoutData);
            Assert.AreEqual(0, hitTestResult.LineLayoutData.LineInParagraphIndex, "可以命中到第二段首行");
        });

        "对包含两段的文本进行命中测试，文本首段和第二段存在首段的段后间距，命中到首段的段后间距，可以返回命中到首段".Test(() =>
        {
            // Arrange
            const double fontSize = 20;
            const double paragraphAfter = 10;
            var textEditorCore = TestHelper.GetLayoutTestTextEditor(fontSize: fontSize);
            // 文本首段和第二段存在首段的段后间距
            textEditorCore.DocumentManager.SetStyleParagraphProperty(
                textEditorCore.DocumentManager.StyleParagraphProperty with
                {
                    ParagraphAfter = paragraphAfter
                });
            // 再添加两段用来测试
            textEditorCore.AppendText("123\nabc");
            // Action
            // 第一段的 Outline 高度为 fontSize + paragraphAfter = 30，于是取 fontSize + paragraphAfter / 2 = 25 确保命中到第一段的段后间距的空白地方
            var point = new TextPoint(10, fontSize + paragraphAfter / 2);
            bool result = textEditorCore.TryHitTest(point, out var hitTestResult);

            // Assert
            Assert.AreEqual(true, result);
            Assert.AreEqual(0, hitTestResult.HitParagraphIndex.Index, "可以命中到首段");
        });
    }

    [ContractTestCase]
    public void TestTryHitTest()
    {
        "文本包含空段，命中空段，可返回命中到行首".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetLayoutTestTextEditor();
            // 第二段是空段
            textEditorCore.AppendText("1\n\n2");
            // Action
            // 命中到第二段行首，也就是 1.5 倍高度，确保命中在第二段中间
            var point = new TextPoint(0.2, TestHelper.LayoutTestCharHeight * 1.5);
            bool result = textEditorCore.TryHitTest(point, out var hitTestResult);

            // Assert
            Assert.AreEqual(true, result);
            Assert.AreEqual(1, hitTestResult.HitParagraphIndex.Index);
            Assert.AreEqual(new CaretOffset(2, isAtLineStart: true /*命中到行首*/), hitTestResult.HitCaretOffset);
        });

        "文本包含一段，在段内范围首个字符之后进行命中测试，可以命中".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetLayoutTestTextEditor();
            textEditorCore.AppendText("abcde");

            // Action
            var point = new TextPoint(TestHelper.LayoutTestCharWidth + 0.2, 5);

            bool result = textEditorCore.TryHitTest(point, out var hitTestResult);

            // Assert
            Assert.AreEqual(true, result);
            // 命中到次字符
            Assert.AreEqual("b", hitTestResult.HitCharData!.CharObject.ToText());
            Assert.AreEqual(new CaretOffset(1), hitTestResult.HitCaretOffset);
            Assert.AreEqual(false, hitTestResult.IsEndOfTextCharacterBounds);
            Assert.AreEqual(false, hitTestResult.IsInLineBoundsNotHitChar);
            Assert.AreEqual(false, hitTestResult.IsOutOfTextCharacterBounds);
        });

        "文本是脏的，命中测试失败".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = _ =>
            {
                // 不执行布局
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 追加内容让文本需要布局
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Action
            var point = new TextPoint(0, 0);

            bool result = textEditorCore.TryHitTest(point, out var hitTestResult);

            // Assert
            Assert.AreEqual(false, result);
            Assert.AreEqual(default, hitTestResult);
        });

        "对空文本进行命中测试，可以命中到文档末尾".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            // Action
            var point = new TextPoint(0, 0);

            bool result = textEditorCore.TryHitTest(point, out var hitTestResult);

            // Assert
            Assert.AreEqual(true, result);
            // 可以命中到文档开始，其实说末尾也对
            Assert.AreEqual(new CaretOffset(0), hitTestResult.HitCaretOffset);
            // 文档没有任何字符，因此是空
            Assert.IsNull(hitTestResult.HitCharData);
            // 命中首段
            Assert.AreEqual(new ParagraphIndex(0), hitTestResult.HitParagraphIndex);
            Assert.IsNotNull(hitTestResult.ParagraphProperty);
            Assert.AreEqual(true, hitTestResult.IsHitSpace);
        });
    }
}