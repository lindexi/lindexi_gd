using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass()]
public class TextEditorCoreEditExtensionTests
{
    [ContractTestCase]
    public void EditAndReplaceTest()
    {
        "对一个空文本编辑器连续两次调用 EditAndReplace 方法，插入新文本，可以将文本追加两次".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            // 对一个空文本编辑器连续两次调用 EditAndReplace 方法，插入新文本
            textEditorCore.EditAndReplace(TestHelper.PlainNumberText);
            textEditorCore.EditAndReplace(TestHelper.PlainNumberText);

            // Assert
            // 可以将文本追加两次
            Assert.AreEqual(TestHelper.PlainNumberText.Length * 2, textEditorCore.DocumentManager.CharCount);
        });

        // todo 还没实现替换功能
        //"全选文本，调用 EditAndReplace 方法插入新文本，可以将选择的文本替换为新文本".Test(() =>
        //{
        //    // Arrange
        //    var textEditorCore = TestHelper.GetTextEditorCore();
        //    // 先插入一点文本，用来全选
        //    textEditorCore.AppendText("123123123");

        //    // 全选文本
        //    textEditorCore.SelectAll();

        //    // Action
        //    // 调用 EditAndReplace 方法插入新文本
        //    textEditorCore.EditAndReplace(TestHelper.PlainNumberText);

        //    // Assert
        //    Assert.AreEqual(TestHelper.PlainNumberText.Length, textEditorCore.DocumentManager.CharCount);
        //});
    }

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
            Assert.AreEqual(TestHelper.PlainNumberText.Length, textEditorCore.DocumentManager.CharCount);
        });
    }
}