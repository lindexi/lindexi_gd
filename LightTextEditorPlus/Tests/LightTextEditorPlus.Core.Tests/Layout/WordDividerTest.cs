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
        // todo 加上行末标点符号换行
        "测试空行强行换行，传入字符串 about 给定宽度只能布局 1 个字符，可以在布局时强行将一个单词拆分为五行".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider().UsingFixedCharSizeCharInfoMeasurer();
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 一个字符的宽度
            var width = 1 * TestHelper.DefaultFixCharWidth;
            textEditorCore.DocumentManager.DocumentWidth = width;

            // Action
            // 添加文本，在单元测试里面将会立刻布局
            textEditorCore.AppendText("about");

            // Assert
            // 预期强行将 about 拆分为五行
            RenderInfoProvider renderInfoProvider = textEditorCore.GetRenderInfo();
            ParagraphRenderInfo paragraphRenderInfo = renderInfoProvider.GetParagraphRenderInfoList().First();
            List<ParagraphLineRenderInfo> paragraphLineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();
            Assert.AreEqual(5, paragraphLineRenderInfoList.Count);
        });

        "测试空行强行换行，传入字符串 about 给定宽度只能布局 3 个字符，可以在布局时强行将一个单词拆分为两行".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider().UsingFixedCharSizeCharInfoMeasurer();
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 三个字符的宽度
            var width = 3 * TestHelper.DefaultFixCharWidth;
            textEditorCore.DocumentManager.DocumentWidth = width;

            // Action
            // 添加文本，在单元测试里面将会立刻布局
            textEditorCore.AppendText("about");

            // Assert
            // 预期强行将 about 拆分为两行
            RenderInfoProvider renderInfoProvider = textEditorCore.GetRenderInfo();
            ParagraphRenderInfo paragraphRenderInfo = renderInfoProvider.GetParagraphRenderInfoList().First();
            List<ParagraphLineRenderInfo> paragraphLineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();
            Assert.AreEqual(2, paragraphLineRenderInfoList.Count);

            Assert.AreEqual("abo", paragraphLineRenderInfoList[0].LineLayoutData.GetText());
            Assert.AreEqual("ut", paragraphLineRenderInfoList[1].LineLayoutData.GetText());
        });

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