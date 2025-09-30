using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout.LayoutUtils.WordDividers;

[TestClass()]
public class GetCaretWordHelperTests
{
    [ContractTestCase()]
    public void GetCaretWordTest()
    {
        "传入 '|abc ' 状态，可以获取 'abc' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset(0), textEditor));

            // Assert
            Assert.AreEqual(new Selection(new CaretOffset(0), "abc".Length), result.WordSelection);
        });

        "传入 '|abc' 状态，可以获取 'abc' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset(0), textEditor));

            // Assert
            Assert.AreEqual(new Selection(new CaretOffset(0), "abc".Length), result.WordSelection);
        });
    }
}