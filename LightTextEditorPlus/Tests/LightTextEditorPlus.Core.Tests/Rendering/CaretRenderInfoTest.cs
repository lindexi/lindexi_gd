using MSTest.Extensions.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.Tests.Rendering;

[TestClass]
public class CaretRenderInfoTest
{
    [ContractTestCase]
    public void GetCaretRenderInfo()
    {
        "获取第二段的文本光标信息，在文本第二段是空段时，可以获取到正确的渲染信息".Test(() =>
        {
            // Arrange
            // 这是空文本，没有任何的布局等信息
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
            Assert.AreEqual(new Rect(0, 15, 0, 15), caretRenderInfo.LineBounds);
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
