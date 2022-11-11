using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass()]
public class TextEditorCoreEditExtensionTests
{
    [ContractTestCase]
    public void InsertTextAfterCurrentCaretOffsetTest()
    {
        "对一个空文本编辑器调用在当前光标之后插入文本方法，可以追加文本".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
#pragma warning disable CS0618
            textEditorCore.InsertTextAfterCurrentCaretOffset(TestHelper.PlainNumberText);
#pragma warning restore CS0618

            // Assert
            Assert.AreEqual(3, textEditorCore.DocumentManager.CharCount);
        });

    }
}