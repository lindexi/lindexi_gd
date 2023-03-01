using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class LayoutTest
{
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
            Assert.AreEqual(1,paragraphLineList1.Count);
            Assert.AreEqual("a", paragraphLineList1[0].Argument.CharList[0].CharObject.ToText());

            var paragraphLineList2 = nextParagraphRenderInfoList[1].GetLineRenderInfoList().ToList();
            Assert.AreEqual(1, paragraphLineList2.Count);
            Assert.AreEqual("b", paragraphLineList2[0].Argument.CharList[0].CharObject.ToText());
        });
    }
}