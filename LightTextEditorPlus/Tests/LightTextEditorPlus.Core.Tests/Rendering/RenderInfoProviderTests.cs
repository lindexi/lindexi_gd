using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;
using LightTextEditorPlus.Core.Utils;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Rendering;

[TestClass()]
public class RenderInfoProviderTests
{
    [ContractTestCase()]
    public void GetSelectionBoundsListTest()
    {
        "传入跨段的选择，可以获取到两条的选择范围".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值方便计算
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            const double charWidth = TestHelper.DefaultFixCharWidth;
            // 一行放 3 个字符
            textEditorCore.DocumentManager.DocumentWidth = charWidth * 3 + 2;
            textEditorCore.AppendText("abcde\r\nfgh");
            /*
             * abc
             * de
             * fgh
             */

            // Action
            var length = "de".Length + TextContext.NewLine.Length + "f".Length;
            var selection = new Selection(new CaretOffset(3), length); // 从 d 到 f 这几个字符的范围
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            var selectionBoundsList = renderInfoProvider.GetSelectionBoundsList(selection);

            // Assert
            // 获取到两行的范围
            Assert.AreEqual(2, selectionBoundsList.Count);

            Assert.AreEqual(0, selectionBoundsList[0].X);
            Assert.AreEqual(30, selectionBoundsList[0].Width);

            Assert.AreEqual(0, selectionBoundsList[1].X);
            Assert.AreEqual(15, selectionBoundsList[1].Width);
        });

        "传入跨行的选择，可以获取到两行的选择范围".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值方便计算
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            const int charWidth = 15;
            // 一行放 3 个字符
            textEditorCore.DocumentManager.DocumentWidth = charWidth * 3 + 2;
            textEditorCore.AppendText("abcdef");

            // Action
            var selection = new Selection(new CaretOffset(1),new CaretOffset(5)); // 从 b 到 e 这几个字符的范围
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            var selectionBoundsList = renderInfoProvider.GetSelectionBoundsList(selection);

            // Assert
            // 获取到两行的范围
            Assert.AreEqual(2, selectionBoundsList.Count);
            Assert.AreEqual(15, selectionBoundsList[0].X);
            Assert.AreEqual(30, selectionBoundsList[0].Width);

            Assert.AreEqual(0, selectionBoundsList[1].X);
            Assert.AreEqual(30, selectionBoundsList[1].Width);
        });

        "传入空白选择，获取选择的范围，可以获取到空列表".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            textEditorCore.AppendText("123\r\n");

            var renderInfoProvider = textEditorCore.GetRenderInfo();

            // Action
            // 传入空白选择，获取选择的范围
            var selection = new Selection();
            var selectionBoundsList = renderInfoProvider.GetSelectionBoundsList(selection);

            // Assert
            // 获取到空列表
            Assert.AreEqual(0, selectionBoundsList.Count);
        });
    }
}