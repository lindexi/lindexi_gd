using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class RenderManagerTest
{
    [ContractTestCase]
    public void TestLineDrawnResult()
    {
        "文本在执行渲染完成之后，可以设置行缓存数据，在下次渲染时使用".Test(() =>
        {
            var testPlatformProvider = new RenderManagerTestPlatformProvider();
            var testRenderManager = testPlatformProvider.TestRenderManager;

            var textEditorCore = new TextEditorCore(testPlatformProvider)
            {
                DocumentManager =
                {
                    DocumentWidth = 1000000
                }
            };

            // 这是用来设置的行缓存数据，在第一次渲染时设置，期望在下次渲染能拿到
            object renderCache = new object();
            textEditorCore.DocumentManager.SetDefaultTextRunProperty<LayoutOnlyRunProperty>(p => p with
            {
                FontSize = 15
            });

            textEditorCore.AppendText("a\r\n");

            Assert.AreEqual(1, testRenderManager.RenderCount);
            Assert.IsNotNull(testRenderManager.CurrentRenderInfoProvider);

            var provider = testRenderManager.CurrentRenderInfoProvider;
            // 追加的是 a\r\n 导致出现两段
            var paragraphRenderInfoList = provider.GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(2, paragraphRenderInfoList.Count);

            // 一段只有一行
            var lineRenderInfoList = paragraphRenderInfoList[0].GetLineRenderInfoList().ToList();
            Assert.AreEqual(1, lineRenderInfoList.Count);

            // 在第一次渲染时设置，行缓存数据，期望在下次渲染能拿到
            var lineDrawnResult = new LineDrawnResult(renderCache);
            lineRenderInfoList[0].SetDrawnResult(lineDrawnResult);

            // 再次变更文本，再次触发渲染
            textEditorCore.AppendText("a");
            // 再次触发渲染，也就是触发渲染次数两次
            Assert.AreEqual(2, testRenderManager.RenderCount);
            provider = testRenderManager.CurrentRenderInfoProvider;
            var nextParagraphRenderInfoList = provider.GetParagraphRenderInfoList().ToList();
            var nextLineRenderInfoList = nextParagraphRenderInfoList[0].GetLineRenderInfoList().ToList();
            // 首段依然只有一行
            Assert.AreEqual(1, lineRenderInfoList.Count);

            var lineDrawingArgument = nextLineRenderInfoList[0].Argument;
            // 渲染过了
            Assert.AreEqual(true, lineDrawingArgument.IsDrawn);
            // 能获取上次渲染设置的缓存
            Assert.AreSame(renderCache, lineDrawingArgument.LineAssociatedRenderData);
        });
    }

    [ContractTestCase]
    public void TestRenderEvent()
    {
        "文本在执行完成布局之后，将会触发一次渲染".Test(() =>
        {
            var testPlatformProvider = new RenderManagerTestPlatformProvider();
            var testRenderManager = testPlatformProvider.TestRenderManager;

            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            textEditorCore.DocumentChanging += delegate
            {
                // 文档变更之前后，是不会触发渲染的
                Assert.AreEqual(0, testRenderManager.RenderCount);
            };

            textEditorCore.DocumentChanged += delegate
            {
                // 文档变更之前后，是不会触发渲染的
                Assert.AreEqual(0, testRenderManager.RenderCount);
            };

            textEditorCore.LayoutCompleted += delegate
            {
                // 布局完成之后，才会开始触发渲染，但是在此事件触发完成之前，渲染是不会触发的。也就是渲染发生在此布局事件触发之后
                Assert.AreEqual(0, testRenderManager.RenderCount);
            };

            textEditorCore.AppendText(TestHelper.PlainLongNumberText);

            // 由于调度器 RequireDispatchUpdateLayout 是立刻执行布局的，因此在 AppendText 方法完成之后，可以立刻获取到渲染结果
            // 文本在执行完成布局之后，将会触发一次渲染
            Assert.AreEqual(1, testRenderManager.RenderCount);
        });
    }
}