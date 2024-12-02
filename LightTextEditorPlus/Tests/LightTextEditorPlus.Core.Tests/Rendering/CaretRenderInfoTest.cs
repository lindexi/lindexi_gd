using MSTest.Extensions.Contracts;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Tests.Rendering;

[TestClass]
public class CaretRenderInfoTest
{
    [ContractTestCase]
    public void GetCaretRenderInfo()
    {
        "调用 GetCaretRenderInfo 传入超过文档范围的光标，抛出异常".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                .UseFixedLineSpacing();
            var text = "abcde";
            textEditorCore.AppendText(text);

            // Assert
            Assert.ThrowsException<HitCaretOffsetOutOfRangeException>(() =>
            {
                // Action
                // 传入超过文档范围的光标
                var caretOffset = new CaretOffset(text.Length + 1);

                var renderInfoProvider = textEditorCore.GetRenderInfo();
                try
                {
                    renderInfoProvider.GetCaretRenderInfo(caretOffset);
                }
                catch (HitCaretOffsetOutOfRangeException e)
                {
                    Assert.AreEqual(caretOffset, e.InputCaretOffset);
                    Assert.AreEqual(text.Length, e.CurrentDocumentCharCount);
                    Assert.IsNotNull(e.ArgumentName);
                    Assert.IsNotNull(e.Message);
                    throw;
                }
            });
        });

        "传入骗人的光标属于行首，实际光标非行首情况下，可以在框架内自动无视光标属于行首属性".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                .UseFixedLineSpacing();
            var charWidth = 15;
            // 设置一行只放下三个字符
            textEditorCore.DocumentManager.DocumentWidth = charWidth * 3 + 0.1;
            // 创建两段的文本
            textEditorCore.AppendText("abcde\r\nfg");

            // Action
            // 将光标设置骗人的行首
            var caretOffset = new CaretOffset("abcde".Length + TextContext.NewLine.Length + "f".Length, isAtLineStart: true);
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider); // 单元测试里是立刻布局，可以立刻获取到渲染信息

            var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(caretOffset);

            // Assert
            // 命中到 f 字符
            Assert.IsNotNull(caretRenderInfo.CharData);
            Assert.AreEqual("f", caretRenderInfo.CharData.CharObject.ToText());

            Assert.AreEqual(0, caretRenderInfo.LineIndex);
            Assert.AreEqual(1, caretRenderInfo.ParagraphIndex);

            Assert.AreEqual(1, caretRenderInfo.HitLineCaretOffset.Offset);
            Assert.AreEqual(0, caretRenderInfo.HitLineCharOffset.Offset);
        });

        "光标处于第二段的段末，可以获取到正确的渲染信息".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                .UseFixedLineSpacing();
            var charWidth = 15;
            // 设置一行只放下三个字符
            textEditorCore.DocumentManager.DocumentWidth = charWidth * 3 + 0.1;
            // 创建两段的文本
            textEditorCore.AppendText("abcde\r\nfg");
            // 此时布局出来的效果如下
            // abc
            // de[回车]
            // fg

            // Action
            // 将光标设置第二段的段末
            var caretOffset = new CaretOffset("abcde".Length + TextContext.NewLine.Length + "fg".Length);
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider); // 单元测试里是立刻布局，可以立刻获取到渲染信息

            var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(caretOffset);

            // Assert
            // 命中到 g 字符
            Assert.IsNotNull(caretRenderInfo.CharData);
            Assert.AreEqual("g", caretRenderInfo.CharData.CharObject.ToText());

            Assert.AreEqual(0, caretRenderInfo.LineIndex);
            Assert.AreEqual(1, caretRenderInfo.ParagraphIndex);

            Assert.AreEqual(2, caretRenderInfo.HitLineCaretOffset.Offset);
            Assert.AreEqual(1, caretRenderInfo.HitLineCharOffset.Offset);

            // 命中到正确的行
            Assert.AreEqual("fg", caretRenderInfo.LineCharDataList.ToText());

            // 命中到最后一个字符，也就是没有后续字符
            Assert.IsNull(caretRenderInfo.GetCharDataInLineAfterCaretOffset());

            Assert.AreSame(textEditorCore, caretRenderInfo.TextEditor);
        });

        "光标处于第二段的段首，可以获取到正确的渲染信息".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                .UseFixedLineSpacing();
            var charWidth = 15;
            // 设置一行只放下三个字符
            textEditorCore.DocumentManager.DocumentWidth = charWidth * 3 + 0.1;
            // 创建两段的文本
            textEditorCore.AppendText("abcde\r\nfg");
            // 此时布局出来的效果如下
            // abc
            // de[回车]
            // fg

            // Action
            // 将光标设置在 f 字符之前
            var caretOffset = new CaretOffset("abcde".Length + TextContext.NewLine.Length, isAtLineStart: true);
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider); // 单元测试里是立刻布局，可以立刻获取到渲染信息

            var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(caretOffset);

            // Assert
            // 命中到 f 字符
            Assert.IsNotNull(caretRenderInfo.CharData);
            Assert.AreEqual("f", caretRenderInfo.CharData.CharObject.ToText());
            // 获取下一个字符
            Assert.AreEqual("g", caretRenderInfo.GetCharDataInLineAfterCaretOffset()!.CharObject.ToText());
            Assert.AreEqual(true, caretRenderInfo.IsLineStart);

            Assert.AreEqual(0, caretRenderInfo.LineIndex);
            Assert.AreEqual(1, caretRenderInfo.ParagraphIndex);

            Assert.AreEqual(0, caretRenderInfo.HitLineCaretOffset.Offset);
            Assert.AreEqual(0, caretRenderInfo.HitLineCharOffset.Offset);
        });

        "光标处于非首行的行首，可以获取到正确的行序号".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                .UseFixedLineSpacing();
            var charWidth = 15;
            // 设置一行只放下三个字符
            textEditorCore.DocumentManager.DocumentWidth = charWidth * 3 + 0.1;
            // 创建一段两行的文本
            textEditorCore.AppendText("abcde");
            // 此时布局出来的效果如下
            // abc
            // de

            // Action
            // 将光标设置在 d 字符之前，行首
            var caretOffset = new CaretOffset(3, isAtLineStart: true);
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider); // 单元测试里是立刻布局，可以立刻获取到渲染信息

            var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(caretOffset);

            // Assert
            Assert.AreEqual(1, caretRenderInfo.LineIndex, "获取到的是首段的末行");
            Assert.AreEqual(0, caretRenderInfo.ParagraphIndex, "获取到的是首段的末行");
            Assert.AreEqual(0, caretRenderInfo.HitLineCaretOffset.Offset);
            Assert.AreEqual(0, caretRenderInfo.HitLineCharOffset.Offset);

            Assert.IsNotNull(caretRenderInfo.CharData);
            Assert.AreEqual("d", caretRenderInfo.CharData.CharObject.ToText());
        });

        "获取第二段的文本光标信息，在文本第二段是空段时，可以获取到正确的渲染信息".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                .UseFixedLineSpacing();

            // Action
            // 追加两段文本，其中第二段是空白，这样就可以获取在文本第二段是空段时的光标
            textEditorCore.AppendText("123\r\n");

            // Assert
            // 由于追加两段文本，刚好当前光标就是测试所需的光标
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider);
            var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(textEditorCore.CurrentCaretOffset);
            Assert.AreEqual(true, caretRenderInfo.IsEmptyParagraph);
            // 一行的高度是 15 也就是第二行就是 (0,15) 的
            Assert.AreEqual(new TextRect(0, 15, 0, 15), caretRenderInfo.LineBounds);

            Assert.AreEqual(0, caretRenderInfo.HitLineCaretOffset.Offset);
            Assert.AreEqual(0, caretRenderInfo.HitLineCharOffset.Offset);

            Assert.AreEqual(0, caretRenderInfo.LineIndex);
            Assert.AreEqual(1, caretRenderInfo.ParagraphIndex);
        });
    }

    [ContractTestCase]
    public void GetCaretRenderInfoEmptyTextEditor()
    {
        "对空文本获取光标信息，可以获取到渲染信息".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider();
            // 这是空文本，没有任何的布局等信息
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // Action
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider);

            // Assert
            var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(textEditorCore.CurrentCaretOffset);
            Assert.AreEqual(textEditorCore.CurrentCaretOffset, caretRenderInfo.CaretOffset);
        });
    }
}
