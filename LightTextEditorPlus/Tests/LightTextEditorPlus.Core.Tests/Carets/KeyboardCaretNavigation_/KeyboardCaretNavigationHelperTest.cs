using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Carets;

[TestClass]
public class KeyboardCaretNavigationHelperTest
{
    [ContractTestCase]
    public void GetPreviousLineCaretOffset()
    {
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

        static TextEditorCore GetTextEditorCore()
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
}