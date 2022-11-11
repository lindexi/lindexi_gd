using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

[TestClass()]
public class CaretTests
{
    [ContractTestCase]
    public void ChangeCaretOffset()
    {
        "修改光标坐标，将会清空当前选择".Test(() =>
        {
            // Arrange
            // 先创建一个包含一些字符的文本，用来设置光标坐标
            var textEditorCore = TestHelper.GetTextEditorCore();
            textEditorCore.AppendText("123123123");
            // 当前是已选择部分内容
            textEditorCore.DocumentManager.CurrentSelection = new Selection(new CaretOffset(1), 2);

            // 当前是有选择的
            Assert.AreEqual(false, textEditorCore.DocumentManager.CurrentSelection.IsEmpty);

            // Action
            // 修改光标坐标
            textEditorCore.DocumentManager.CurrentCaretOffset = new CaretOffset(1);

            // Assert
            // 将会清空当前选择
            Assert.AreEqual(new CaretOffset(1), textEditorCore.DocumentManager.CurrentSelection.StartOffset);
            Assert.AreEqual(true, textEditorCore.DocumentManager.CurrentSelection.IsEmpty);
        });

        "修改光标坐标，将会同步设置选择范围".Test(() =>
        {
            // Arrange
            // 先创建一个包含一些字符的文本，用来设置光标坐标
            var textEditorCore = TestHelper.GetTextEditorCore();
            textEditorCore.AppendText("123123123");

            // Action
            // 修改光标坐标
            textEditorCore.DocumentManager.CurrentCaretOffset = new CaretOffset(1);

            // Assert
            // 将会同步设置选择范围
            Assert.AreEqual(new CaretOffset(1), textEditorCore.DocumentManager.CurrentSelection.StartOffset);
            Assert.AreEqual(true, textEditorCore.DocumentManager.CurrentSelection.IsEmpty);
        });
    }
}