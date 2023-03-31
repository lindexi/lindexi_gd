using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorCoreTest
{
    [ContractTestCase]
    public void TestCreate()
    {
        "测试文本的创建".Test(() =>
        {
            var textEditorCore = TestHelper.GetTextEditorCore();

            // 没有异常，那就是符合预期
            Assert.IsNotNull(textEditorCore);
        });
    }

    [ContractTestCase]
    public void GetHitParagraphData()
    {
        "命中到文本的段落的换车符，可以自动修改为命中段末".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 加上一个测试信息，用来有两个段落，这样才能命中
            textEditorCore.AppendText("12\r\n");

            // Action
            var caretOffset = new CaretOffset(textEditorCore.CurrentCaretOffset.Offset - 1);
            var paragraphManager = textEditorCore.DocumentManager.ParagraphManager;
            var result = paragraphManager.GetHitParagraphData(caretOffset);

            var hitCharData = result.GetHitCharData();
            Assert.IsNotNull(hitCharData);

            // Assert
            Assert.AreEqual(2, result.HitOffset.Offset);
            // 命中到 '2' 字符
            Assert.AreEqual("2", hitCharData.CharObject.ToText());
        });
    }

    [ContractTestCase]
    public void EventArrange()
    {
        "文本编辑的事件触发是 DocumentChanging DocumentChanged LayoutCompleted 顺序".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var raiseCount = 0;

            textEditorCore.DocumentChanging += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(0, raiseCount);
                raiseCount++;
            };

            textEditorCore.DocumentChanged += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(1, raiseCount);
                raiseCount = 2;
            };

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(2, raiseCount);
                raiseCount = 3;
            };

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            Assert.AreEqual(3, raiseCount);
        });
    }

    [ContractTestCase]
    public void GetDocumentBounds()
    {
        "给文本编辑器追加一段纯文本，在布局渲染完成之后，可以获取到文档的尺寸".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                // 可以获取到文档的尺寸
                var documentBounds = textEditorCore.GetDocumentLayoutBounds();
                Assert.AreEqual(true, documentBounds.Width > 0);
                Assert.AreEqual(true, documentBounds.Height > 0);
            };

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);
        });
    }
}