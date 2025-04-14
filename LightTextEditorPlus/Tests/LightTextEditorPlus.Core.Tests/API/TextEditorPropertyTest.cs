using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorPropertyTest
{
    [ContractTestCase]
    public void SetArrangingTypeTest()
    {
        "设置文本从横排到竖排，文本状态是脏的".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider();
            var updateLayoutCount = 0;
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = action =>
            {
                if (updateLayoutCount == 0)
                {
                    // 只有首次才进行布局，执行完布局之后，文本就不是脏的了
                    // 预期第二次进入布局的原因是 ArrangingType 发生了变化，此时特意不进行布局，方便单元测试测量出文本的脏状态
                    action();
                }
                updateLayoutCount++;
            };

            var textEditorCore = new TextEditorCore(testPlatformProvider);
            // 随便加一些文本
            textEditorCore.AppendText(TestHelper.PlainLongNumberText);
            // 此时将会触发一次布局，且布局之后文本就不是脏的了
            Assert.AreEqual(1, updateLayoutCount);
            Assert.IsFalse(textEditorCore.IsDirty);

            // Action
            // 设置文本从横排到竖排
            textEditorCore.ArrangingType = ArrangingType.Vertical;

            // Assert
            // 预期再次请求布局，且由于 RequireDispatchUpdateLayoutHandler 委托不执行布局，文本是脏的
            Assert.AreEqual(2, updateLayoutCount);
            Assert.IsTrue(textEditorCore.IsDirty);
        });
    }

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
