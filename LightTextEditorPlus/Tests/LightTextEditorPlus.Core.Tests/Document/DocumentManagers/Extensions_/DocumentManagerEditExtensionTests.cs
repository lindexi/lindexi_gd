using LightTextEditorPlus.Core.Document.DocumentManagers;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

[TestClass()]
public class DocumentManagerEditExtensionTests
{
    [ContractTestCase]
    public void InsertTextAfterCurrentCaretOffsetTest()
    {
        "对一个空文本编辑器调用在当前光标之后插入文本方法，可以追加文本".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.DocumentManager.InsertTextAfterCurrentCaretOffset(TestHelper.PlainNumberText);

            // Assert
            Assert.AreEqual(3, textEditorCore.DocumentManager.CharCount);
        });

    }
}