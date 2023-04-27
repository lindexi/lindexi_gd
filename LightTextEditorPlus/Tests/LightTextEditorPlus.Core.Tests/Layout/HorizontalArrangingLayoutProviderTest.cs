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
            var size = new Size(CharWidth * 5,  CharHeight * 2);
            Assert.AreEqual(size, paragraphLayoutData.Size);
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