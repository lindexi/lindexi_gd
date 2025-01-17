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
    public void TryHitTest()
    {
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
