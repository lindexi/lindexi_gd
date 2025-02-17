using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorPropertyTest
{
    [ContractTestCase]
    public void GetNewCaretOffsetTest()
    {
        @"文本有两行 '123123|abc' 首行 6 个字符，光标在首行末，获取 下 键，可以返回在文档末的光标，不会出现越界错误".Test(() =>
        {
            // Arrange
            TestPlatformProvider testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.UsingFixedCharSizeCharInfoMeasurer();

            TextEditorCore textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            textEditorCore.DocumentManager.DocumentWidth = textEditorCore.DocumentManager.StyleRunProperty.FontSize * 6 + 1;
            textEditorCore.AppendText("123123abc");
            // 光标在首行末
            textEditorCore.CurrentCaretOffset = new CaretOffset("123123".Length);

            // Action
            // 获取 下 键
            CaretOffset newCaretOffset = textEditorCore.GetNewCaretOffset(CaretMoveType.DownByLine);

            // Assert
            // 显然第二行为 'abc' 字符，只有三个字符，没有 6 个字符，光标在文档末，需要要求返回第二行的第 3 个字符的光标，而不是直接将首行的 6 个字符的光标叠加下一行进行返回
            // 可以返回在文档末的光标
            Assert.AreEqual("123123abc".Length, newCaretOffset.Offset);
        });

    }
}
