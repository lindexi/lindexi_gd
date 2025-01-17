using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Carets;

[TestClass]
public class KeyboardCaretNavigationHelperTest
{
    [ContractTestCase]
    public void GetNextCharacterCaretOffset()
    {
        "文本包含两段每段两行，光标在首段末，方向键向右，光标在末段首".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg|
            // ABCDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在首段末
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标在末段首
            // abcde
            // fg
            // |ABCDE
            // FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength, textEditorCore.CurrentCaretOffset.Offset);
            Assert.AreEqual(true, textEditorCore.CurrentCaretOffset.IsAtLineStart);
        });

        "文本包含两段每段两行，光标在末段末行首行倒数第1个字符后，方向键向右，光标在末段末行首".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // ABCD|E
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段末行首行倒数第1个字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "ABCD".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标在末段末行首
            // abcde
            // fg
            // ABCDE
            // |FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDE".Length, textEditorCore.CurrentCaretOffset.Offset);
            Assert.AreEqual(true, textEditorCore.CurrentCaretOffset.IsAtLineStart);
        });

        "文本包含两段每段两行，光标在末段末行首字符后，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // ABCDE
            // F|G
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDEF".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标坐标正确
            // abcde
            // fg
            // ABCDE
            // FG|
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDEFG".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段首行末，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // ABCDE|
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDE".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标坐标正确
            // abcde
            // fg
            // ABCDE
            // F|G
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDEF".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段末行首，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // ABCDE
            // |FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDE".Length, isAtLineStart: true);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标坐标正确
            // abcde
            // fg
            // ABCDE
            // F|G
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDEF".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段次字符后，方向键向右，光标坐标在末段首字符后".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // AB|CDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "AB".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标坐标正确
            // abcde
            // fg
            // ABC|DE
            // FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "ABC".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段首字符后，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // A|BCDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "A".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标坐标正确
            // abcde
            // fg
            // AB|CDE
            // FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "AB".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段首，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            // 文本包含两段每段两行
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // |ABCDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末行首
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 光标坐标正确
            // abcde
            // fg
            // A|BCDE
            // FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "A".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段两行，光标在末行首，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            // 文本包含一段两行
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // |fg
            textEditorCore.AppendText("abcdefg");

            // 光标在末行首
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcde".Length, isAtLineStart: true);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 在 f 字符之后。敲黑板，不在 f 之前哦，自己玩玩就知道了
            // abcde
            // f|g
            Assert.AreEqual("abcdef".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段两行，光标在首行末，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            // 文本包含一段两行
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde|
            // fg
            textEditorCore.AppendText("abcdefg");

            // 光标在首行末
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcde".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            // 在 f 字符之后，而不是之前
            // abcde
            // f|g
            Assert.AreEqual("abcdef".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在末字符之后，方向键向右，光标不变且无异常".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在末字符之后
            textEditorCore.CurrentCaretOffset = new CaretOffset(3);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            Assert.AreEqual(3, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在次字符之后，方向键向右，光标在文档末".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在次字符之后
            textEditorCore.CurrentCaretOffset = new CaretOffset(2);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            Assert.AreEqual(3, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在首字符之后，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在首字符之后
            textEditorCore.CurrentCaretOffset = new CaretOffset(1);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            Assert.AreEqual(2, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在文档首，方向键向右，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在文档首
            textEditorCore.CurrentCaretOffset = new CaretOffset(0);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            Assert.AreEqual(1, textEditorCore.CurrentCaretOffset.Offset);
        });

        "空文本，光标在文档首，方向键向右，光标不变且无异常".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 空文本，啥都没有

            // Action
            textEditorCore.MoveCaret(CaretMoveType.RightByCharacter);

            // Assert
            Assert.AreEqual(0, textEditorCore.CurrentCaretOffset.Offset);
        });
    }

    [ContractTestCase]
    public void GetPreviousCharacterCaretOffset()
    {
        "文本包含两段每段两行，光标在末段末行首字符后，方向键向左，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // ABCDE
            // F|G
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDEF".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标是在 D 字符之后哦
            // abcde
            // fg
            // ABCDE
            // |FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDE".Length, textEditorCore.CurrentCaretOffset.Offset);
            Assert.AreEqual(true, textEditorCore.CurrentCaretOffset.IsAtLineStart);
        });

        "文本包含两段每段两行，光标在末段末行首，方向键向左，光标坐标正确".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // ABCDE
            // |FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "ABCDE".Length, isAtLineStart: true);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标是在 D 字符之后哦
            // abcde
            // fg
            // ABCD|E
            // FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "ABCD".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段次字符后，方向键向左，光标坐标在末段首字符后".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // AB|CDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "AB".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标坐标在末段首字符后
            // abcde
            // fg
            // A|BCDE
            // FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + "A".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段首字符后，方向键向左，光标坐标在末段首".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // A|BCDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末段次字符后
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength + "A".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标坐标在末段首字符后
            // abcde
            // fg
            // |ABCDE
            // FG
            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含两段每段两行，光标在末段首，方向键向左，光标坐标在首段末".Test(() =>
        {
            // Arrange
            // 文本包含两段每段两行
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // fg
            // |ABCDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // 光标在末行首
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcdefg".Length + ParagraphData.DelimiterLength);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标坐标在首段末
            // abcde
            // fg|
            // ABCDE
            // FG
            Assert.AreEqual("abcdefg".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段两行，光标在末行首，方向键向左，光标坐标正确".Test(() =>
        {
            // Arrange
            // 文本包含一段两行
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde
            // |fg
            textEditorCore.AppendText("abcdefg");

            // 光标在末行首
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcde".Length, isAtLineStart: true);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 在 d 字符之后。敲黑板，不在 e 之后哦，自己玩玩就知道了
            // abcd|e
            // fg
            Assert.AreEqual("abcd".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段两行，光标在首行末，方向键向左，光标坐标正确".Test(() =>
        {
            // Arrange
            // 文本包含一段两行
            var textEditorCore = GetTextEditorCore();
            // 排版出来的是
            // abcde|
            // fg
            textEditorCore.AppendText("abcdefg");

            // 光标在首行末
            textEditorCore.CurrentCaretOffset = new CaretOffset("abcde".Length);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 在 d 字符之后
            Assert.AreEqual("abcd".Length, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在末字符之后，方向键向左，光标在次字符之后".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在末字符之后
            textEditorCore.CurrentCaretOffset = new CaretOffset(3);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标在次字符之后
            Assert.AreEqual(2, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在次字符之后，方向键向左，光标在首字符之后".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在次字符之后
            textEditorCore.CurrentCaretOffset = new CaretOffset(2);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标在首字符之后
            Assert.AreEqual(1, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在首字符之后，方向键向左，光标在文档首".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在首字符之后
            textEditorCore.CurrentCaretOffset = new CaretOffset(1);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            // 光标在文档首
            Assert.AreEqual(0, textEditorCore.CurrentCaretOffset.Offset);
        });

        "文本包含一段三个字符，光标在文档首，方向键向左，光标不变且无异常".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            textEditorCore.AppendText("123");

            // 光标在文档首
            textEditorCore.CurrentCaretOffset = new CaretOffset(0);

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            Assert.AreEqual(0, textEditorCore.CurrentCaretOffset.Offset);
        });

        "空文本，光标在文档首，方向键向左，光标不变且无异常".Test(() =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();
            // 空文本，啥都没有

            // Action
            textEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);

            // Assert
            Assert.AreEqual(0, textEditorCore.CurrentCaretOffset.Offset);
        });
    }

    [ContractTestCase]
    public void GetNextLineCaretOffset()
    {
        "文本包含两段文本，每段两行，光标在首段末行，方向键下一行，光标可到末段首行".Test((int offset) =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();

            // 排版出来的是
            // abcde
            // fg|
            // ABCDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // Action
            // 光标在首段末行
            var caretOffset = new CaretOffset(offset + "abcde".Length,
                isAtLineStart: offset == 0);
            textEditorCore.CurrentCaretOffset = caretOffset;
            // 方向键下一行
            textEditorCore.MoveCaret(CaretMoveType.DownByLine);

            // Assert
            // 光标可到末段首行
            var caretRenderInfo = textEditorCore.GetRenderInfo().GetCurrentCaretRenderInfo();
            Assert.AreEqual(0, caretRenderInfo.LineIndex);
            Assert.AreEqual(new ParagraphIndex(1), caretRenderInfo.ParagraphIndex);

            Assert.AreEqual("abcdefg".Length + ParagraphData.DelimiterLength + offset, textEditorCore.CurrentCaretOffset.Offset);
        }).WithArguments(0, 1, 2);

        "光标在首段首行，方向键下一行，光标可到首段末行".Test((int offset) =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();

            // 排版出来的是
            // abcde|
            // fg
            textEditorCore.AppendText("abcdefg");

            // Action
            // 光标在首段首行
            var caretOffset = new CaretOffset(offset);
            textEditorCore.CurrentCaretOffset = caretOffset;
            // 方向键下一行
            textEditorCore.MoveCaret(CaretMoveType.DownByLine);

            // Assert
            // 光标可到首段末行
            var caretRenderInfo = textEditorCore.GetRenderInfo().GetCaretRenderInfo(textEditorCore.CurrentCaretOffset);
            Assert.AreEqual(1, caretRenderInfo.LineIndex);
            Assert.AreEqual(new ParagraphIndex(0), caretRenderInfo.ParagraphIndex);

            if (offset < 2)
            {
                Assert.AreEqual("abcde".Length + offset, textEditorCore.CurrentCaretOffset.Offset);
            }
            else
            {
                Assert.AreEqual("abcdefg".Length, textEditorCore.CurrentCaretOffset.Offset);
            }
        }).WithArguments(0, 1, 2, 3, 4, 5);

        "文本包含两段每段一行，光标在末段末行，方向键下一行，光标不变".Test((int offset) =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();

            // 排版出来的是
            // abcde
            // ABCDE
            textEditorCore.AppendText("abcde\nABCDE");

            // Action
            // 光标在末段末行
            var caretOffset = new CaretOffset(offset + "abcde".Length + ParagraphData.DelimiterLength, isAtLineStart: offset == 0);
            textEditorCore.CurrentCaretOffset = caretOffset;
            // 方向键下一行
            textEditorCore.MoveCaret(CaretMoveType.DownByLine);

            // Assert
            // 光标不变
            Assert.AreEqual(caretOffset, textEditorCore.CurrentCaretOffset);
        }).WithArguments(0, 1, 2, 3, 4, 5);

        "文本包含一段两行，光标在末段末行，方向键下一行，光标不变".Test((int offset) =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();

            // 排版出来的是
            // abcde
            // fg|
            textEditorCore.AppendText("abcdefg");

            // Action
            // 光标在末段末行
            var caretOffset = new CaretOffset(offset + "abcde".Length, isAtLineStart: offset == 0);
            textEditorCore.CurrentCaretOffset = caretOffset;
            // 方向键下一行
            textEditorCore.MoveCaret(CaretMoveType.DownByLine);

            // Assert
            // 光标不变
            Assert.AreEqual(caretOffset, textEditorCore.CurrentCaretOffset);
        }).WithArguments(0, 1, 2);
    }

    [ContractTestCase]
    public void GetPreviousLineCaretOffset()
    {
        "文本包含两段文本，每段两行，光标在末段首行，方向键上一行，光标可到首段末行".Test((int offset) =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();

            // 排版出来的是
            // abcde
            // fg
            // ABCDE
            // FG
            textEditorCore.AppendText("abcdefg\nABCDEFG");

            // Action
            // 光标在末段首行
            var caretOffset = new CaretOffset(offset + "abcdefg".Length + ParagraphData.DelimiterLength,
                isAtLineStart: offset == 0);
            textEditorCore.CurrentCaretOffset = caretOffset;
            // 方向键上一行
            textEditorCore.MoveCaret(CaretMoveType.UpByLine);

            // Assert
            // 光标可到首段末行
            var caretRenderInfo = textEditorCore.GetRenderInfo().GetCaretRenderInfo(textEditorCore.CurrentCaretOffset);
            Assert.AreEqual(1, caretRenderInfo.LineIndex);
            Assert.AreEqual(new ParagraphIndex(0), caretRenderInfo.ParagraphIndex);

            if (offset > 2)
            {
                // 在 B 之后，上一行都会在 g 之后
                Assert.AreEqual("abcdefg".Length, textEditorCore.CurrentCaretOffset.Offset);
            }
            else
            {
                Assert.AreEqual("abcde".Length + offset, textEditorCore.CurrentCaretOffset.Offset);
            }
        }).WithArguments(0, 1, 2, 3, 4, 5);

        "光标在首段末行，方向键上一行，光标可到首段首行".Test((int offset) =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();

            // 排版出来的是
            // abcde
            // fg
            textEditorCore.AppendText("abcdefg");

            // Action
            // 光标在首段首行
            var caretOffset = new CaretOffset(offset, isAtLineStart: offset == 5);
            textEditorCore.CurrentCaretOffset = caretOffset;
            // 方向键上一行
            textEditorCore.MoveCaret(CaretMoveType.UpByLine);

            // Assert
            // 光标可到首段首行
            var caretRenderInfo = textEditorCore.GetRenderInfo().GetCaretRenderInfo(textEditorCore.CurrentCaretOffset);
            Assert.AreEqual(0, caretRenderInfo.LineIndex);
            Assert.AreEqual(new ParagraphIndex(0), caretRenderInfo.ParagraphIndex);

            Assert.AreEqual(offset - 5, textEditorCore.CurrentCaretOffset.Offset);
        }).WithArguments(5, 6, 7);

        "光标在首段首行，方向键上一行，光标不变".Test((int offset) =>
        {
            // Arrange
            var textEditorCore = GetTextEditorCore();

            // 排版出来的是
            // abcde
            // fg
            textEditorCore.AppendText("abcdefg");

            // Action
            // 光标在首段首行
            var caretOffset = new CaretOffset(offset);
#pragma warning disable CS0618
            textEditorCore.MoveCaret(caretOffset);
#pragma warning restore CS0618
            // 方向键上一行
            textEditorCore.MoveCaret(CaretMoveType.UpByLine);

            // Assert
            // 光标不变
            Assert.AreEqual(caretOffset, textEditorCore.CurrentCaretOffset);
        }).WithArguments(0, 1, 2, 3, 4, 5);
    }

    private static TextEditorCore GetTextEditorCore()
    {
        // 采用 FixCharSizePlatformProvider 固定数值，光标导航测试里强依赖布局结果
        var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
            .UseFixedLineSpacing();

        var charWidth = 15;
        // 设置一行只放下五个字符
        textEditorCore.DocumentManager.DocumentWidth = charWidth * 5 + 0.1;
        return textEditorCore;
    }
}
