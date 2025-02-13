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
            Assert.AreEqual(new CaretOffset(2, isAtLineStart: true/*命中到行首*/), hitTestResult.HitCaretOffset);
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
