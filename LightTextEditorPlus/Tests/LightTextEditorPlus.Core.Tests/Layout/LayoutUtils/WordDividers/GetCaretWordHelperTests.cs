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
        "[GetCaretWord] 传入 '中文，|汉字 ' 状态，可以获取 '汉字' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("中文，汉字 ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset("中文，".Length), textEditor));

            // Assert
            string text = textEditor.GetText(result.WordSelection);
            Assert.AreEqual("汉字", text);
        });

        "[GetCaretWord] 传入 '中文|，汉字 ' 状态，可以获取 '中文' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("中文，汉字 ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset("中文".Length), textEditor));

            // Assert
            string text = textEditor.GetText(result.WordSelection);
            Assert.AreEqual("中文", text);
        });

        "[GetCaretWord] 传入 '123|,abc ' 状态，可以获取 '123' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("123,abc ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset("123".Length), textEditor));

            // Assert
            string text = textEditor.GetText(result.WordSelection);
            Assert.AreEqual("123", text);
        });

        "[GetCaretWord] 传入 '123,|abc ' 状态，可以获取 'abc' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("123,abc ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset("123 ".Length), textEditor));

            // Assert
            string text = textEditor.GetText(result.WordSelection);
            Assert.AreEqual("abc", text);
        });

        "[GetCaretWord] 传入 '123| abc ' 状态，可以获取 '123' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("123 abc ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset("123".Length), textEditor));

            // Assert
            string text = textEditor.GetText(result.WordSelection);
            Assert.AreEqual("123", text);
        });

        "[GetCaretWord] 传入 '123 |abc ' 状态，可以获取 'abc' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("123 abc ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset("123 ".Length), textEditor));

            // Assert
            string text = textEditor.GetText(result.WordSelection);
            Assert.AreEqual("abc", text);
        });

        "[GetCaretWord] 传入 ' |abc ' 状态，可以获取 'abc' 单词".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText(" abc ");

            // Action
            // 设置光标在开头
            var result = GetCaretWordHelper.GetCaretWord(new GetCaretWordArgument(new CaretOffset(1), textEditor));

            // Assert
            string text = textEditor.GetText(result.WordSelection);
            Assert.AreEqual("abc", text);
        });

        "[GetCaretWord] 传入 '|abc ' 状态，可以获取 'abc' 单词".Test(() =>
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

        "[GetCaretWord] 传入 '|abc' 状态，可以获取 'abc' 单词".Test(() =>
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