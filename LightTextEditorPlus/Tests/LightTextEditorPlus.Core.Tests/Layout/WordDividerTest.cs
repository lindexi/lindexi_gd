using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout;

[TestClass]
public class WordDividerTest
{
    [ContractTestCase]
    public void LayoutLongWord()
    {
        "测试后拆分方式，传入字符串包含两个单词，单词之间使用字符分割，其中第二个单词是超长单词，可以排版为三行".Test((char c) =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider().UsingFixedCharSizeCharInfoMeasurer();
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 配置文本一行只能放下 5 个字符
            // 加上 0.1 处理误差
            var width = 5 * TestHelper.DefaultFixCharWidth + 0.1;
            textEditorCore.DocumentManager.DocumentWidth = width;

            // Action
            // 添加文本，在单元测试里面将会立刻布局
            textEditorCore.AppendText($"aa{c}aaaaaa");

            // Assert
            // 可以排版为三行
            // aa
            // aaaaa
            // a
            var lineList = textEditorCore.GetRenderInfo().GetParagraphRenderInfoList().First().GetLineRenderInfoList().ToList();
            Assert.AreEqual(3, lineList.Count);
            Assert.AreEqual($"aa{c}", lineList[0].Argument.CharList.ToText());
            Assert.AreEqual("aaaaa", lineList[1].Argument.CharList.ToText());
            Assert.AreEqual("a", lineList[2].Argument.CharList.ToText());
        }).WithArguments(',', '.', '!', '(', ')', '#', '1', '2', ':', ';', '十');

        "测试后拆分方式，传入字符串包含两个单词，其中第二个单词是超长单词，可以排版为三行".Test(() =>
        {
            // 对于此类型的字符串，如 `aa aaaaaa` 来说，允许单词断行排版的情况下，排版两行即可完成
            // 如果加上单词不分割策略和 后拆分 方式，那将需要排版为三行，第二个单词排版在二三行
            // 这个行为和 Office 的 PPT 相同

            // Arrange
            var testPlatformProvider = new TestPlatformProvider().UsingFixedCharSizeCharInfoMeasurer();
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 配置文本一行只能放下 5 个字符
            // 加上 0.1 处理误差
            var width = 5 * TestHelper.DefaultFixCharWidth + 0.1;
            textEditorCore.DocumentManager.DocumentWidth = width;

            // Action
            // 添加文本，在单元测试里面将会立刻布局
            textEditorCore.AppendText("aa aaaaaa");

            // Assert
            // 可以排版为三行
            // aa
            // aaaaa
            // a
            var lineList = textEditorCore.GetRenderInfo().GetParagraphRenderInfoList().First().GetLineRenderInfoList().ToList();
            Assert.AreEqual(3, lineList.Count);
            Assert.AreEqual("aa ", lineList[0].Argument.CharList.ToText());
            Assert.AreEqual("aaaaa", lineList[1].Argument.CharList.ToText());
            Assert.AreEqual("a", lineList[2].Argument.CharList.ToText());
        });
    }

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
            Assert.AreEqual(2, paragraphLineRenderInfoList.Count);

            Assert.AreEqual("aa ", paragraphLineRenderInfoList[0].LineLayoutData.GetText());
            Assert.AreEqual("about", paragraphLineRenderInfoList[1].LineLayoutData.GetText());
        });
    }
}