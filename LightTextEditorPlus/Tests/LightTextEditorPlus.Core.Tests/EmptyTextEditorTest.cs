using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class EmptyTextEditorTest
{
    [ContractTestCase]
    public void TestIsDirty()
    {
        "空文本创建出来时，文本就是脏的，即使没有做任何的变更".Test(() =>
        {
            var textEditorCore = new TextEditorCore(new TestPlatformProvider());
            Assert.AreEqual(true, textEditorCore.IsDirty, "按照约定，默认创建出来的文本是脏的，需要布局完成之后，才不是脏的");
        });
    }

    [ContractTestCase]
    public void GetHitParagraphData()
    {
        "对空文本获取 GetHitParagraphData 方法，获取之后，将会创建默认的一行文本".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            var result = textEditorCore.DocumentManager.ParagraphManager.GetHitParagraphData(new CaretOffset(0));

            // Assert
            // 获取之后，将会创建默认的一行文本
            Assert.AreEqual(1, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList().Count);

            // 命中到 0 的光标
            Assert.AreEqual(new CaretOffset(0), result.InputCaretOffset);
            Assert.AreEqual(new ParagraphCaretOffset(0), result.HitOffset);

            // 文本字符数量依然是 0 个
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);
        });
    }
}
