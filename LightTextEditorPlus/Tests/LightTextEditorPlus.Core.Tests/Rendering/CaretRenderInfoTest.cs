using MSTest.Extensions.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.Tests.Rendering;

[TestClass]
public class CaretRenderInfoTest
{
    [ContractTestCase]
    public void GetCaretRenderInfoEmptyTextEditor()
    {
        "对空文本获取光标信息，可以获取到渲染信息".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider();
            // 这是空文本，没有任何的布局等信息
            var textEditorCore = new TextEditorCore(testPlatformProvider);

            // Action
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider);

            // Assert
            var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(textEditorCore.CurrentCaretOffset);
            Assert.AreEqual(textEditorCore.CurrentCaretOffset, caretRenderInfo.CaretOffset);
        });
    }
}
