using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout;

[TestClass]
public class HorizontalArrangingLayoutProviderTest
{
    private const double FontSize = 20;
    private const double CharWidth = FontSize;
    private const double CharHeight = FontSize;
    private const double LineSpacing = 1.5;

    [ContractTestCase]
    public void LayoutParagraph()
    {
        "文本包含一段两行，布局尺寸正确".Test(() =>
        {
            // Arrange
            var textEditor = GetTestTextEditor();

            // Action
            // 文本包含一段两行
            // abcde
            // fg
            textEditor.AppendText("abcdefg");

            // Assert
            var paragraphRenderInfo = textEditor.GetRenderInfo().GetParagraphRenderInfoList().First();
            var paragraphLayoutData = paragraphRenderInfo.ParagraphLayoutData;
            // 布局有两行，一行宽度是 5 个字符，每个字符 20 的宽度
            // 一行高度是 CharHeight 的高度
            var size = new Size(CharWidth * 5, CharHeight * 2);
            Assert.AreEqual(size, paragraphLayoutData.Size);
        });
    }

    // todo 加上段前段后布局测试

    /// <summary>
    /// 倍数行距的文本排版
    /// </summary>
    [ContractTestCase]
    public void LayoutLineSpacing()
    {
        "文本包含两段，设置 1.5 倍行距，在首段追加一行，影响文本第二段起始坐标，文本第二段布局正确".Test(() =>
        {
            // Arrange
            var textEditor = GetTestTextEditor();

            // 设置 1.5 倍行距
            textEditor.DocumentManager.SetParagraphProperty(0, textEditor.DocumentManager.CurrentParagraphProperty with
            {
                LineSpacing = LineSpacing
            });

            // 文本包含两段
            // abcde \n
            // ABCDE
            // FG
            textEditor.AppendText("abcde\r\nABCDEFG");

            // 获取第二段的起始坐标，用于后续判断是否改变
            Point startPoint = textEditor.GetRenderInfo().GetParagraphRenderInfoList().ToList()[1].ParagraphLayoutData.StartPoint;

            // Action
            // 在首段追加一行
            // 追加后是
            // abcde
            // fg \n
            // ABCDE
            // FG
            textEditor.EditAndReplace("fg", new Selection(new CaretOffset("abcde".Length), 0));

            // Assert
            // 影响文本第二段起始坐标，文本第二段布局正确
            var paragraphRenderInfo = textEditor.GetRenderInfo().GetParagraphRenderInfoList().ToList()[1];
            var paragraphLayoutData = paragraphRenderInfo.ParagraphLayoutData;
            Assert.AreNotEqual(startPoint, paragraphLayoutData.StartPoint);
            // 布局有两行，一行宽度是 5 个字符，每个字符 20 的宽度
            // 一行高度是 LineSpacing * CharHeight 的高度
            var size = new Size(CharWidth * 5, LineSpacing * CharHeight * 2);
            Assert.AreEqual(size, paragraphLayoutData.Size);

            var lineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();
            // 第一行起始就是首段的末尾，首段的高度是两行的高度，一行高度是 LineSpacing * CharHeight 的高度
            Assert.AreEqual(new Point(0, LineSpacing * CharHeight * 2), lineRenderInfoList[0].LineLayoutData.CharStartPoint);
            // 一行的宽度是 CharWidth * 字符数量
            Assert.AreEqual(new Size(CharWidth* "ABCDE".Length, LineSpacing * CharHeight), lineRenderInfoList[0].LineLayoutData.LineCharSize);

            // 第二行的起始等于首段的末尾加第一行高度
            Assert.AreEqual(new Point(0, LineSpacing * CharHeight * 2 + LineSpacing * CharHeight), lineRenderInfoList[1].LineLayoutData.CharStartPoint);
            Assert.AreEqual(new Size(CharWidth * "FG".Length, LineSpacing * CharHeight), lineRenderInfoList[1].LineLayoutData.LineCharSize);
        });

        "文本包含两段，设置 1.5 倍行距，在首段追加，修改之后不影响文本第二段起始坐标，文本第二段布局正确".Test(() =>
        {
            // Arrange
            var textEditor = GetTestTextEditor();

            // 设置 1.5 倍行距
            textEditor.DocumentManager.SetParagraphProperty(0, textEditor.DocumentManager.CurrentParagraphProperty with
            {
                LineSpacing = LineSpacing
            });

            // 文本包含两段
            textEditor.AppendText("abcdefg\r\nABCDEFG");

            // 获取第二段的起始坐标，用于后续判断是否改变
            Point startPoint = textEditor.GetRenderInfo().GetParagraphRenderInfoList().ToList()[1].ParagraphLayoutData.StartPoint;

            // Action
            // 在首段追加
            // 追加后是
            // abcde
            // fg123 \n
            // ABCDE
            // FG
            textEditor.EditAndReplace("123", new Selection(new CaretOffset("abcdefg".Length), 0));

            // Assert
            // 修改之后不影响文本第二段起始坐标，文本第二段布局正确
            var paragraphRenderInfo = textEditor.GetRenderInfo().GetParagraphRenderInfoList().ToList()[1];
            var paragraphLayoutData = paragraphRenderInfo.ParagraphLayoutData;
            Assert.AreEqual(startPoint, paragraphLayoutData.StartPoint);
            // 布局有两行，一行宽度是 5 个字符，每个字符 20 的宽度
            // 一行高度是 LineSpacing * CharHeight 的高度
            var size = new Size(CharWidth * 5, LineSpacing * CharHeight * 2);
            Assert.AreEqual(size, paragraphLayoutData.Size);
        });

        "文本包含一段一行，设置 1.5 倍行距，采用 FakeLineSpacing 算法，字号 20 的文本为 30 行高".Test(() =>
        {
            // Arrange
            var textEditor = GetTestTextEditor();

            var text = "123";

            // Action
            // 文本包含一段一行
            textEditor.AppendText(text);
            // 设置 1.5 倍行距
            textEditor.DocumentManager.SetParagraphProperty(0, textEditor.DocumentManager.CurrentParagraphProperty with
            {
                LineSpacing = LineSpacing
            });

            // Assert
            var renderInfoProvider = textEditor.GetRenderInfo();
            var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();
            var paragraphRenderInfo = paragraphRenderInfoList[0];
            Assert.AreEqual(new Point(0, 0), paragraphRenderInfo.ParagraphLayoutData.StartPoint);

            Assert.AreEqual(new Size(CharWidth * text.Length, CharHeight * LineSpacing), paragraphRenderInfo.ParagraphLayoutData.Size);

            var lineRenderInfo = paragraphRenderInfo.GetLineRenderInfoList().First();
            var lineLayoutData = lineRenderInfo.LineLayoutData;
            Assert.AreEqual(new Point(0, 0), lineLayoutData.CharStartPoint);
            Assert.AreEqual(new Size(CharWidth * text.Length, CharHeight * LineSpacing), lineLayoutData.LineCharSize);
        });
    }

    private static TextEditorCore GetTestTextEditor()
    {
        var testPlatformProvider = new TestPlatformProvider();
        // 使用固定字符尺寸计算，返回字符尺寸等于字号，方便计算
        testPlatformProvider.UsingFixedCharSizeCharInfoMeasurer();
        testPlatformProvider.UseFakeLineSpacingCalculator();

        var textEditor = TestHelper.GetTextEditorCore(testPlatformProvider);
        // 设置 20 字号，方便行距计算
        textEditor.DocumentManager.SetDefaultTextRunProperty<LayoutOnlyRunProperty>(t => t.FontSize = FontSize);
        // 设置一行能布局 5 个字
        textEditor.DocumentManager.DocumentWidth = CharWidth * 5 + 0.1;
        return textEditor;
    }
}