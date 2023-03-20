using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorUndoRedoTest
{
    [ContractTestCase]
    public void UndoRedo()
    {
        "追加文本之后，可以通过撤销重做撤回更改".Test(() =>
        {
            // Arrange
            var testTextEditorUndoRedoProvider = new TestTextEditorUndoRedoProvider();
            var textEditorCore = TestHelper.GetTextEditorCore(new TestPlatformProvider()
            {
                UndoRedoProvider = testTextEditorUndoRedoProvider
            });

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            // 会自动加入撤销重做
            Assert.AreEqual(1, testTextEditorUndoRedoProvider.UndoOperationList.Count);

            // 撤销一下
            testTextEditorUndoRedoProvider.Undo();

            // 撤销完成，那就是空文本了
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);

            // 继续重做，那就是存在文本
            testTextEditorUndoRedoProvider.Redo();
            Assert.AreEqual(TestHelper.PlainNumberText,textEditorCore.GetText());
        });
    }
}