using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
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
        "给空文本追加 \\n1 字符串，文本创建两段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，这样比较好计算
                .UseFixedLineSpacing();

            // Action
            textEditorCore.AppendText("\n1");

            // Assert
            Assert.AreEqual(2, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList().Count);
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
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，这样比较好计算
                .UseFixedLineSpacing();

            // Action
            textEditorCore.AppendText("a\r\n\r\nb");
            textEditorCore.AppendText("c");

            // 没有抛出异常就是符合预期
            // Assert

            //a
            //\n
            //\nbc
            // 根据 FixCharSizePlatformProvider 提供的参数，空行高度 15 和每个字符都是 15 的宽度和高度
            // 一共三行，也就是高度是 15 * 3 = 45 的高度
            // 最大宽度为第二行的内容，也就是 bc 两个字符，一共是 15 * 2 = 30 的宽度
            ParagraphRenderInfo paragraphRenderInfo = textEditorCore.GetRenderInfo().GetParagraphRenderInfoList().Last();
            var lastLine = paragraphRenderInfo.GetLineRenderInfoList().Last();
            Assert.AreEqual(30, lastLine.LineLayoutData.CharStartPoint.Y);
            Assert.AreEqual(15, lastLine.LineLayoutData.LineCharTextSize.Height);

            Assert.AreEqual(new TextRect(0, 0, 15 * 2, 15 * 3), textEditorCore.GetDocumentLayoutBounds());
        });

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
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                .UseFixedLineSpacing();

            // Action
            textEditorCore.AppendText("\r\n");
            textEditorCore.AppendText("a");
            // 没有抛出异常就是符合预期

            // Assert
            // 文本的样子是：
            //  -
            // | |
            // |a|
            //  -
            // 高度 = 一行 15 高度 + 一行 15 高度 = 30 高度
            // 宽度 = 字符 a 宽度 = 15 宽度
            Assert.AreEqual(new TextRect(0, 0, 15, 30), textEditorCore.GetDocumentLayoutBounds());
        });

        "给文本追加一个 \\r\\n 字符串，文本可以分两段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider()).UseFixedLineSpacing();

            // Action
            textEditorCore.AppendText("\r\n");

            // Assert
            Assert.AreEqual(0, textEditorCore.GetDocumentLayoutBounds().Width);
            Assert.AreEqual(30, textEditorCore.GetDocumentLayoutBounds().Height);
        });
    }
}