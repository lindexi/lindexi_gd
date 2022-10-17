using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorInsertTest
{
    [ContractTestCase]
    public void InsertCenterTest()
    {
        "在现有的 123 文本的中间，在 2 后面插入 456 字符串，可以成功插入".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            textEditorCore.AppendText("123");
            var textRunManager = textEditorCore.DocumentManager.TextRunManager;

            // Action

            /* 
             * 0 1 2 3
             * | | | |
             *  1 2 3
             */
            // 在 2 后面插入 456 字符串
            // 在字符串 “2” 后面的光标选择就是 `new CaretOffset(2)` 的内容
            var selection = new Selection(new CaretOffset(2), 0);
            // 插入 456 字符串
            var textRun = new TextRun("456");
            textRunManager.Replace(selection,textRun);

            // Assert
            // 在 123 文本的中间，在 2 后面插入 456 字符串
            // 预期的字符串就是 12 456 3
            var text =  textRunManager.ParagraphManager.GetText();
            Assert.AreEqual("124563",text);
        });
    }
}