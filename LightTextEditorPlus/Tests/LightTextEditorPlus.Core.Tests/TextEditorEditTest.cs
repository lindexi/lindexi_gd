using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorEditTest
{
    [ContractTestCase]
    public void Remove()
    {
        // todo 删除超过文本字符数量
        "对文本调用 Remove 传入空选择，啥都不会发生".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 随便传入一点文本，然后调用删除空白选择
            textEditorCore.AppendText("12");

            // 预期啥都不会发生，也就是不会触发布局等变更事件
            textEditorCore.DocumentChanging += (sender, args) =>
            {
                Assert.Fail("对文本调用 Remove 传入空选择，啥都不会发生");
            };

            // Action
            textEditorCore.Remove(new Selection(new CaretOffset(0), 0));

            // Assert
            // 不会删除字符
            Assert.AreEqual(2, textEditorCore.DocumentManager.CharCount);
        });
    }

    [ContractTestCase]
    public void Delete()
    {
        "对文本调用 Delete 删除，可以删除光标之后一个字符".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 输入两个字符，用来调用 Delete 删除
            textEditorCore.AppendText("12");
            // 然后将光标移动到第零个字符后面，用于按下 Delete 删除
            // 第零个字符后面的光标坐标是 1 的值
            textEditorCore.CurrentCaretOffset = new CaretOffset(1);

            // Action
            textEditorCore.Delete();

            // Assert
            Assert.AreEqual(1, textEditorCore.DocumentManager.CharCount);
            var paragraphLineRenderInfo = textEditorCore.GetRenderInfo().GetParagraphRenderInfoList().First().GetLineRenderInfoList().First();
            var text = paragraphLineRenderInfo.LineLayoutData.GetCharList()[0].CharObject.ToText();
            // 在第零个字符后面，删除 "2" 这个字符
            Assert.AreEqual("1", text);
        });

        "对空文本调用 Delete 删除，啥都不会发生".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            // 啥都不做，这就是一个空文本
            textEditorCore.DocumentChanging += (sender, args) =>
            {
                Assert.Fail("对空文本调用 Delete 删除，啥都不会发生");
            };

            // Action
            textEditorCore.Delete();

            // Assert
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);
        });
    }

    [ContractTestCase]
    public void Backspace()
    {
        // 在段首执行 Backspace 退格，可以删除段，和前面一段合成一段

        "对空段执行 Backspace 退格，可以删除空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加一个字符加一段，用于后续删除
            textEditorCore.AppendText("1\r\n");
            // 追加之后，光标在文档最后，也就是在空段
            // 此时不需要修改光标了

            // Action
            // 对空段执行 Backspace 退格
            textEditorCore.Backspace();
        });

        "对只有一个字符的文本执行 Backspace 退格，可以删除所有文本".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加一个字符，用于后续删除
            textEditorCore.AppendText("1");

            // Action
            textEditorCore.Backspace();

            // Assert
            // 可以删除所有文本，等于文本字符数量是空
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);
            // 删除之后，依然存在一段
            Assert.AreEqual(1, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList().Count);
            Assert.AreEqual(1, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList()[0].LineLayoutDataList.Count);
        });
    }
}