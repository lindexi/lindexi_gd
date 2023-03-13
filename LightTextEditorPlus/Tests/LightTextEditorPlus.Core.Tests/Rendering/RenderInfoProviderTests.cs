using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Rendering;

[TestClass()]
public class RenderInfoProviderTests
{
    [ContractTestCase()]
    public void GetSelectionBoundsListTest()
    {
        "传入空白选择，获取选择的范围，可以获取到空列表".Test(() =>
        {
            // Arrange
            // 采用 FixCharSizePlatformProvider 固定数值
            var textEditorCore = new TextEditorCore(new FixCharSizePlatformProvider());
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