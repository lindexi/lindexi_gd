using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorAppendTest
{
    [ContractTestCase]
    public void AppendText()
    {
        "给文本编辑器连续两次追加文本，可以将后追加的文本，追加在最后".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.AppendText("123");
            textEditorCore.AppendText("456");

            // Assert
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider);

            var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();

            // 可以排版出来1段1行
            Assert.AreEqual(1, paragraphRenderInfoList.Count);

            Assert.AreEqual("123456", paragraphRenderInfoList.First().GetLineRenderInfoList().First().LineLayoutData.GetText());
        });

        @"给文本编辑器追加 123\r\n123\r\n 文本，可以排版出来三段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                var renderInfoProvider = textEditorCore.GetRenderInfo();
                Assert.IsNotNull(renderInfoProvider);

                var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();

                // 可以排版出来三段
                Assert.AreEqual(3, paragraphRenderInfoList.Count);
            };

            // Action
            textEditorCore.AppendText("123\r\n123\r\n");
        });

        "给文本编辑器追加两段纯文本，可以排版出来两段两行".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                var renderInfoProvider = textEditorCore.GetRenderInfo();
                Assert.IsNotNull(renderInfoProvider);

                var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();

                // 可以排版出来两段两行
                Assert.AreEqual(2, paragraphRenderInfoList.Count);

                foreach (var paragraphRenderInfo in paragraphRenderInfoList)
                {
                    var paragraphLineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();
                    Assert.AreEqual(1, paragraphLineRenderInfoList.Count);

                    Assert.AreEqual("123", paragraphLineRenderInfoList[0].LineLayoutData.GetText());
                }
            };

            // Action
            // 给文本编辑器追加两段纯文本
            textEditorCore.AppendText("123\r\n123");
        });

        "给文本编辑器追加一段纯文本，先触发 DocumentChanging 再触发 DocumentChanged 事件".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var raiseCount = 0;

            textEditorCore.DocumentChanging += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(0, raiseCount);
                raiseCount++;
            };

            textEditorCore.DocumentChanged += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(1, raiseCount);
                raiseCount = 2;
            };

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            Assert.AreEqual(2, raiseCount);
        });
    }


    [ContractTestCase]
    public void AppendBreakParagraph()
    {
        "对空段的文本追加字符串，如对 \\r\\n 追加 a 字符，不会抛出异常".Test(() =>
        {
            // Arrange
            /*
                System.InvalidOperationException:“Sequence contains no elements”
                	System.Linq.dll!System.Linq.ThrowHelper.ThrowNoElementsException()
                	System.Linq.dll!System.Linq.Enumerable.Last<LightTextEditorPlus.Core.Document.LineLayoutData> (System.Collections.Generic.IEnumerable<LightTextEditorPlus.Core.Document.LineLayoutData> source)	未知
                	LightTextEditorPlus.Core.dll! LightTextEditorPlus.Core.Layout.HorizontalArrangingLayoutProvider.GetNextParagraphLineStartPoint (LightTextEditorPlus.Core.Document.ParagraphData paragraphData) 行 412	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Layout.ArrangingLayoutProvider.UpdateLayout() 行 468	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Layout.LayoutManager.UpdateLayout() 行 46	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.TextEditorCore.UpdateLayout() 行 144	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Platform.PlatformProvider.RequireDispatchUpdateLayout (System.Action textLayout) 行 24	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.TextEditorCore.DocumentManager_DocumentChanged(object sender,  System.EventArgs e) 行 132	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Document.DocumentManager.AppendText(string text) 行 250	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.TextEditorCore.AppendText(string text) 行 14	C#
             */
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            // Action
            textEditorCore.AppendText("\r\n");
            textEditorCore.AppendText("a");
            // 没有抛出异常就是符合预期

            // 文本的样子是：
            //  -
            // | |
            // |a|
            //  -
            // 高度 = 一行 15 高度 + 一行 15 高度 = 30 高度
            // 宽度 = 字符 a 宽度 = 15 宽度
            Assert.AreEqual(new Rect(0, 0, 15, 30), textEditorCore.GetDocumentLayoutBounds());
        });
        "在包含空段的文本追加字符串，如 a\\r\\n\\r\\nb 再追加 c 字符，文本可以布局多段".Test(() =>
        {
            // Arrange
            /*
                System.InvalidOperationException:“Sequence contains no elements”
                	System.Linq.dll!System.Linq.ThrowHelper.ThrowNoElementsException()
                	System.Linq.dll!System.Linq.Enumerable.Last<LightTextEditorPlus.Core.Document.LineLayoutData> (System.Collections.Generic.IEnumerable<LightTextEditorPlus.Core.Document.LineLayoutData> source)	未知
                	LightTextEditorPlus.Core.dll! LightTextEditorPlus.Core.Layout.HorizontalArrangingLayoutProvider.GetNextParagraphLineStartPoint (LightTextEditorPlus.Core.Document.ParagraphData paragraphData) 行 412	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Layout.ArrangingLayoutProvider.UpdateLayout() 行 468	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Layout.LayoutManager.UpdateLayout() 行 46	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.TextEditorCore.UpdateLayout() 行 144	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Platform.PlatformProvider.RequireDispatchUpdateLayout (System.Action textLayout) 行 24	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.TextEditorCore.DocumentManager_DocumentChanged(object sender,  System.EventArgs e) 行 132	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.Document.DocumentManager.AppendText(string text) 行 250	C#
                	LightTextEditorPlus.Core.dll!LightTextEditorPlus.Core.TextEditorCore.AppendText(string text) 行 14	C#
             */
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            // Action
            textEditorCore.AppendText("a\r\n\r\nb");
            textEditorCore.AppendText("c");

            // 没有抛出异常就是符合预期

            // 根据 FixCharSizePlatformProvider 提供的参数，空行高度 15 和每个字符都是 15 的宽度和高度
            // 一共三行，也就是高度是 15 * 3 = 45 的高度
            // 最大宽度为第二行的内容，也就是 bc 两个字符，一共是 15 * 2 = 30 的宽度
            Assert.AreEqual(new Rect(0, 0, 15 * 2, 15 * 3), textEditorCore.GetDocumentLayoutBounds());
        });

        "给文本追加一个 \\r\\n 字符串，文本可以分两段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            // Action
            textEditorCore.AppendText("\r\n");

            // Assert
            Assert.AreEqual(0, textEditorCore.GetDocumentLayoutBounds().Width);
            Assert.AreEqual(30, textEditorCore.GetDocumentLayoutBounds().Height);
        });
    }
}

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
        "在段首执行 Backspace 退格，可以删除段，和前面一段合成一段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加两段，用于后续删除
            textEditorCore.AppendText("1\r\n2");
            // 移动光标在段首
            textEditorCore.CurrentCaretOffset = new CaretOffset(3);

            // Action
            // 在段首执行 Backspace 退格
            textEditorCore.Backspace();

            // Assert
            // 可以删除段，和前面一段合成一段
            Assert.AreEqual("12", textEditorCore.GetText());
        });

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

            // Assert
            // 删除之后，就只剩下一个字符
            Assert.AreEqual("1", textEditorCore.GetText());
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