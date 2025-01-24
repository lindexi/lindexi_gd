using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorInsertTest
{
    [ContractTestCase]
    public void BreakLineTest()
    {
        "在现有的文本为 123 字符串的行首插入两次换行，可以排版三段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider()).UseFixedLineSpacing();
            // 预设文本，用于测试
            textEditorCore.AppendText("123");

            // Action
            // 在行首插入
            textEditorCore.CurrentCaretOffset = new CaretOffset(0);
            textEditorCore.EditAndReplace("\n");
            textEditorCore.EditAndReplace("\n");

            // Assert
            // 排版三段
            var paragraphRenderInfoList = textEditorCore.GetRenderInfo().GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(3, paragraphRenderInfoList.Count);
            var lineLayoutData = paragraphRenderInfoList[2].GetLineRenderInfoList().FirstOrDefault().LineLayoutData;
            Assert.AreEqual(30, lineLayoutData.CharStartPoint.Y);
        });

        "文本连续追加两个换行，可以排版三段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider()).UseFixedLineSpacing();

            // Action
            // 原本文本就有一个空段，在一个空文本按下两次回车，就是有三段
            textEditorCore.AppendText("\n");
            textEditorCore.AppendText("\n");

            // Assert
            // 排版三段
            var paragraphRenderInfoList = textEditorCore.GetRenderInfo().GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(3, paragraphRenderInfoList.Count);
            var lineLayoutData = paragraphRenderInfoList[2].GetLineRenderInfoList().FirstOrDefault().LineLayoutData;
            Assert.AreEqual(30, lineLayoutData.CharStartPoint.Y);
        });

        "在现有的 123 文本的 12 中间，插入换行符，可以创建两段文本，光标在字符 2 前面".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 插入预设的文本
            textEditorCore.AppendText("123");
            // 设置光标在文本的 12 中间
            textEditorCore.CurrentCaretOffset = new CaretOffset(1);

            // Action
            // 插入换行符
            textEditorCore.EditAndReplace("\r\n");

            // Assert
            // 光标在字符 2 前面。也就是光标在 1\n 后面
            CaretOffset currentCaretOffset = textEditorCore.CurrentCaretOffset;
            Assert.AreEqual("1\n".Length, currentCaretOffset.Offset);
            Assert.AreEqual(true, currentCaretOffset.IsAtLineStart);
        });
    }

    [ContractTestCase]
    public void InsertCenterTest()
    {
        "在现有的 123 文本的中间，在 2 后面插入 456 字符串，可以成功插入".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            textEditorCore.AppendText("123");

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
            textEditorCore.DocumentManager.EditAndReplaceRun(selection, textRun);

            // Assert
            // 在 123 文本的中间，在 2 后面插入 456 字符串
            // 预期的字符串就是 12 456 3
            var text = textEditorCore.GetText();
            Assert.AreEqual("124563", text);
        });
    }
}
