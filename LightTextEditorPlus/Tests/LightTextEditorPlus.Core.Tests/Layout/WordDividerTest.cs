using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout;

[TestClass]
public class WordDividerTest
{
    [ContractTestCase]
    public void LayoutLine()
    {
        "测试标准 aa about 断行布局，可以符合布局预期进行布局，布局成两行，整个 about 放在一行".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider().UsingFixedCharSizeCharInfoMeasurer();
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 一行的宽度刚好就是一个 about 加 1 的宽度，也就是放入 "aa " 之后就不够放入
            var width = ("about".Length + 1) * TestHelper.DefaultFixCharWidth;
            textEditorCore.DocumentManager.DocumentWidth = width;

            // Action
            // 添加文本，在单元测试里面将会立刻布局
            textEditorCore.AppendText("aa about");

            // Assert
            RenderInfoProvider renderInfoProvider = textEditorCore.GetRenderInfo();
            ParagraphRenderInfo paragraphRenderInfo = renderInfoProvider.GetParagraphRenderInfoList().First();
            List<ParagraphLineRenderInfo> paragraphLineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();
            Assert.AreEqual(2,paragraphLineRenderInfoList.Count);

            Assert.AreEqual("aa ", paragraphLineRenderInfoList[0].LineLayoutData.GetText());
            Assert.AreEqual("about", paragraphLineRenderInfoList[1].LineLayoutData.GetText());
        });
    }
}