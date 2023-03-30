using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout;

[TestClass]
public class LayoutTest
{
    [ContractTestCase]
    public void LayoutParagraph()
    {
        "文本包含两段，布局过一次，在第一段进行追加，第二段只需修改坐标不需要重新布局".Test(() =>
        {
            var renderManagerTestPlatformProvider = new RenderManagerTestPlatformProvider();
            var textEditorCore = new TextEditorCore(renderManagerTestPlatformProvider);

            // 加入两段文本，用于测试
            textEditorCore.AppendText("abc\r\ndef");

            // 先给他一个缓存数据，这样就可以知道第二段是不是重新布局了
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(2, paragraphRenderInfoList.Count);
            // 获取第二段的第一行。第二段其实也只有一行
            // 在这一行里面设置缓存。如果后续第二段不需要重新布局，那就依然能获取到第二段的第一行的缓存
            var lineList = paragraphRenderInfoList[1].GetLineRenderInfoList().ToList();
            object cache = new object();
            lineList[0].SetDrawnResult(new LineDrawnResult(cache));

            // 在第一段进行追加，第二段只需修改坐标不需要重新布局
            textEditorCore.EditAndReplaceRun(new TextRun("1"), new Selection(new CaretOffset(3), 0));

            // 预期没有重新布局第二段，也就是放入第二段的缓存没有被干掉
            renderInfoProvider = textEditorCore.GetRenderInfo();
            lineList = renderInfoProvider.GetParagraphRenderInfoList().ToList()[1].GetLineRenderInfoList().ToList();
            Assert.AreSame(cache, lineList[0].Argument.LineAssociatedRenderData);
        });
    }

    [ContractTestCase]
    public void ChangeDocumentOnUpdatingLayout()
    {
        $"在布局的过程中，修改了文档内容，将会抛出 {nameof(ChangeDocumentOnUpdatingLayoutException)} 错误".Test(() =>
        {
            var testWholeLineLayouter = new TestWholeLineLayouter();
            var testPlatformProvider = new TestPlatformProvider()
            {
                WholeLineLayouter = testWholeLineLayouter
            };
            var textEditorCore = new TextEditorCore(testPlatformProvider);

            // 配置这个 WholeLineLayouter 将会在布局的时候，修改文档内容
            testWholeLineLayouter.LayoutWholeLineFunc = _ =>
            {
                // 其实在这里就炸了
                textEditorCore.AppendText(TestHelper.PlainNumberText);

                return default;
            };

            // 修改文本，触发布局，然后布局过程触发修改文本
            Assert.ThrowsException<ChangeDocumentOnUpdatingLayoutException>(() =>
            {
                textEditorCore.AppendText(TestHelper.PlainNumberText);
            });
        });
    }

    [ContractTestCase]
    public void LayoutTwoParagraph()
    {
        "分两次追加，第一次追加 `a\\r\\n` 第二次追加 `b` 内容，可以排版出两段两行".Test(() =>
        {
            var testPlatformProvider = new RenderManagerTestPlatformProvider();
            var testRenderManager = testPlatformProvider.TestRenderManager;

            var textEditorCore = new TextEditorCore(testPlatformProvider);

            textEditorCore.AppendText("a\r\n");

            // 追加的是 a\r\n 导致出现两段
            Assert.IsNotNull(testRenderManager.CurrentRenderInfoProvider);
            var paragraphRenderInfoList = testRenderManager.CurrentRenderInfoProvider.GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(2, paragraphRenderInfoList.Count);

            // 第二次追加，预期可以排版
            textEditorCore.AppendText("b");

            var provider = testRenderManager.CurrentRenderInfoProvider;
            var nextParagraphRenderInfoList = provider.GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(2, nextParagraphRenderInfoList.Count);
            var paragraphLineList1 = nextParagraphRenderInfoList[0].GetLineRenderInfoList().ToList();
            Assert.AreEqual(1, paragraphLineList1.Count);
            Assert.AreEqual("a", paragraphLineList1[0].Argument.CharList[0].CharObject.ToText());

            var paragraphLineList2 = nextParagraphRenderInfoList[1].GetLineRenderInfoList().ToList();
            Assert.AreEqual(1, paragraphLineList2.Count);
            Assert.AreEqual("b", paragraphLineList2[0].Argument.CharList[0].CharObject.ToText());
        });
    }
}