using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

/// <summary>
/// 光标测试
/// </summary>
/// 移动光标核心实现在 <see cref="KeyboardCaretNavigationHelper"/> 类里
[TestClass]
public class TextEditorCaretTest
{
    [ContractTestCase]
    public void TestGetLeftByCharacterCaretOffset()
    {
        "[GetLeftByCharacterCaretOffset] 传入 'a|bc' 状态，获取左光标，光标向左一个字符".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc");
            // 设置光标在开头
            textEditor.CurrentCaretOffset = new CaretOffset("a".Length);

            // Action
            // 获取左光标
            textEditor.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标向左一个字符
            Assert.AreEqual(0, textEditor.CurrentCaretOffset.Offset);
        });

        "[GetLeftByCharacterCaretOffset] 传入 '|abc' 状态，获取左光标，光标无变化".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc");
            // 设置光标在开头
            textEditor.CurrentCaretOffset = new CaretOffset(0);

            // Action
            // 获取左光标
            textEditor.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标无变化
            Assert.AreEqual(0, textEditor.CurrentCaretOffset.Offset);
        });
    }

    [ContractTestCase]
    public void TestGetRightByCharacterCaretOffset()
    {
        "[GetRightByCharacterCaretOffset] 传入 'a|bc' 状态，获取右光标，光标向右一个字符".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc");
            // 设置光标在 'a' 之后
            textEditor.CurrentCaretOffset = new CaretOffset("a".Length);

            // Action
            // 获取右光标
            textEditor.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标向右一个字符
            Assert.AreEqual(2, textEditor.CurrentCaretOffset.Offset);
        });

        "[GetRightByCharacterCaretOffset] 传入 '|abc' 状态，获取右光标，光标向右一个字符".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc");
            // 设置光标在开头
            textEditor.CurrentCaretOffset = new CaretOffset(0);

            // Action
            // 获取右光标
            textEditor.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标向右一个字符
            Assert.AreEqual(1, textEditor.CurrentCaretOffset.Offset);
        });

        "[GetRightByCharacterCaretOffset] 传入 'abc|' 状态，获取右光标，光标无变化".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc");
            // 设置光标在文档末
            textEditor.CurrentCaretOffset = new CaretOffset(3);

            // Action
            // 获取右光标
            textEditor.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标无变化
            Assert.AreEqual(3, textEditor.CurrentCaretOffset.Offset);
        });
    }

    [ContractTestCase]
    public void TestGetPreviousWordCaretOffset()
    {
        "[GetPreviousWordCaretOffset] 传入 'abc 1|23' 状态，获取左光标单词，可以返回 123 的开头".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("abc 1".Length);

            // Action
            textEditor.MoveCaret(CaretMoveType.LeftByWord);

            // Assert
            // 空格后，返回前一个单词末尾
            Assert.AreEqual("abc ".Length, textEditor.CurrentCaretOffset.Offset);
        });

        "[GetPreviousWordCaretOffset] 传入 'abc |123' 状态，获取左光标单词，可以返回首个单词末尾".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("abc ".Length);

            // Action
            textEditor.MoveCaret(CaretMoveType.LeftByWord);

            // Assert
            // 空格后，返回前一个单词末尾
            Assert.AreEqual("abc".Length, textEditor.CurrentCaretOffset.Offset);
        });

        "[GetPreviousWordCaretOffset] 传入 'abc| 123' 状态，获取左光标单词，可以返回单词开头".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("abc".Length);

            // Action
            textEditor.MoveCaret(CaretMoveType.LeftByWord);

            // Assert
            Assert.AreEqual(0, textEditor.CurrentCaretOffset.Offset);
        });

        "[GetPreviousWordCaretOffset] 传入 'a|bc 123' 状态，获取左光标单词，可以返回单词开头".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("a".Length);

            // Action
            textEditor.MoveCaret(CaretMoveType.LeftByWord);

            // Assert
            Assert.AreEqual(0, textEditor.CurrentCaretOffset.Offset);
        });
    }

    [ContractTestCase]
    public void TestGetNextWordCaretOffset()
    {
        "[GetNextWordCaretOffset] 传入 'abc |123' 状态，获取右光标单词，可以返回 'abc 123|' 状态".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("abc |123".IndexOf('|'));

            // Action
            textEditor.MoveCaret(CaretMoveType.RightByWord);

            // Assert
            Assert.AreEqual("abc 123|".IndexOf('|'), textEditor.CurrentCaretOffset.Offset);
        });

        "[GetNextWordCaretOffset] 传入 'abc| 123' 状态，获取右光标单词，可以返回 'abc |123' 状态".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("abc| 123".IndexOf('|'));

            // Action
            textEditor.MoveCaret(CaretMoveType.RightByWord);

            // Assert
            Assert.AreEqual("abc |123".IndexOf('|'), textEditor.CurrentCaretOffset.Offset);
        });

        "[GetNextWordCaretOffset] 传入 '|abc 123' 状态，获取右光标单词，可以返回单词末尾".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("|abc 123".IndexOf('|'));

            // Action
            textEditor.MoveCaret(CaretMoveType.RightByWord);

            // Assert
            Assert.AreEqual("abc| 123".IndexOf('|'), textEditor.CurrentCaretOffset.Offset);
        });

        "[GetNextWordCaretOffset] 传入 'a|bc 123' 状态，获取右光标单词，可以返回单词末尾".Test(() =>
        {
            // Arrange
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            textEditor.AppendText("abc 123");

            textEditor.CurrentCaretOffset = new CaretOffset("a|bc 123".IndexOf('|'));

            // Action
            textEditor.MoveCaret(CaretMoveType.RightByWord);

            // Assert
            Assert.AreEqual("abc| 123".IndexOf('|'), textEditor.CurrentCaretOffset.Offset);
        });
    }
}