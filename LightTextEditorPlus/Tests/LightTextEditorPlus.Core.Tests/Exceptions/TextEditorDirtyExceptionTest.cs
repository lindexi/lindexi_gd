using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Exceptions;

[TestClass]
public class TextEditorDirtyExceptionTest
{
    [ContractTestCase]
    public void ThrowException()
    {
        "在文本布局完成之前，获取文本布局信息，抛出 TextEditorDirtyException 异常".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            textEditorCore.DocumentChanging += (sender, args) =>
            {
                // 在文本布局完成之前

                Assert.ThrowsException<TextEditorDirtyException>(() =>
                {
                    // 抛出 TextEditorDirtyException 异常
                    textEditorCore.GetRenderInfo();
                });
            };

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);
        });
    }
}