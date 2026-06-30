using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

[TestClass]
public sealed class SlideStreamingPipelineTests
{
    [TestMethod(DisplayName = "同步 ProcessIncrementalText 已移除，改为 async ProcessIncrementalTextAsync")]
    public async Task ProcessIncrementalTextAsync_SingleFragment_TriggersFragmentReceived()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page/>", context);

        // Assert
        Assert.HasCount(1, fragments);
        Assert.AreEqual("<Page/>", fragments[0]);
    }

    [TestMethod(DisplayName = "验证不完整的片段不会触发事件")]
    public async Task ProcessIncrementalTextAsync_PartialFragment_NoEvent()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Panel Id=\"header\">", context);

        // Assert
        Assert.IsEmpty(fragments);
    }

    [TestMethod(DisplayName = "验证多个连续片段全部触发")]
    public async Task ProcessIncrementalTextAsync_MultipleFragments_AllTriggered()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page><Panel Id=\"a\"/></Page><Rect Id=\"b\"/>", context);

        // Assert
        Assert.HasCount(2, fragments);
    }

    [TestMethod(DisplayName = "验证片段合并后 CurrentMergedXml 更新")]
    public async Task ProcessIncrementalTextAsync_MergedXml_UpdatedAfterFragment()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page/>", context);

        // Assert
        Assert.Contains("<Page", pipeline.CurrentMergedXml);
    }

    [TestMethod(DisplayName = "验证流结束合并剩余内容并渲染")]
    public async Task ProcessStreamEndAsync_FlushesRemaining_RendersMergedXml()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\"/></Page>", context);
        await pipeline.ProcessStreamEndAsync(context);

        // Assert：ProcessIncrementalTextAsync 触发 1 次实时渲染，ProcessStreamEndAsync 触发 1 次最终渲染
        Assert.HasCount(2, renderedResults);
    }

    [TestMethod(DisplayName = "验证流每次片段合并后触发实时渲染（带节流）")]
    public async Task ProcessIncrementalTextAsync_RealTimeRender_TriggeredAfterEachFragment()
    {
        // Arrange
        var pipeline = CreatePipeline(minRenderInterval: TimeSpan.Zero);
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\"/></Page>", context);

        // Assert：片段合并后立即触发渲染（无需等待 ProcessStreamEndAsync）
        Assert.HasCount(1, renderedResults);
    }

    [TestMethod(DisplayName = "验证渲染节流：间隔内多次合并只渲染一次")]
    public async Task ProcessIncrementalTextAsync_RenderThrottle_SkipsRapidRenders()
    {
        // Arrange
        var pipeline = CreatePipeline(minRenderInterval: TimeSpan.FromSeconds(10));
        var context = new SlideMlPipelineContext();
        var renderedCount = 0;
        pipeline.Rendered += _ => renderedCount++;

        // Act：快速连续合并两个片段
        await pipeline.ProcessIncrementalTextAsync("<Page><Panel Id=\"a\"/></Page>", context);
        await pipeline.ProcessIncrementalTextAsync("<Panel Id=\"a\"><Rect Id=\"b\"/></Panel>", context);

        // Assert：第二个片段因节流被跳过
        Assert.AreEqual(1, renderedCount);
    }

    [TestMethod(DisplayName = "验证最终渲染忽略节流")]
    public async Task ProcessStreamEndAsync_FinalRender_IgnoresThrottle()
    {
        // Arrange
        var pipeline = CreatePipeline(minRenderInterval: TimeSpan.FromSeconds(10));
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act：先触发一次渲染（占上次渲染时间）
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\"/></Page>", context);
        var countAfterFirst = renderedResults.Count;

        // 最终渲染忽略节流，必定触发
        await pipeline.ProcessStreamEndAsync(context);

        // Assert：最终渲染一定发生
        Assert.AreEqual(countAfterFirst + 1, renderedResults.Count);
    }

    [TestMethod(DisplayName = "验证空流不触发渲染")]
    public async Task ProcessStreamEndAsync_EmptyStream_NoRender()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedCount = 0;
        pipeline.Rendered += _ => renderedCount++;

        // Act
        await pipeline.ProcessStreamEndAsync(context);

        // Assert
        Assert.AreEqual(0, renderedCount);
    }

    [TestMethod(DisplayName = "验证 Reset 清空状态")]
    public async Task Reset_ClearsState()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        await pipeline.ProcessIncrementalTextAsync("<Page/>", context);

        // Act
        pipeline.Reset();

        // Assert
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml));
    }

    [TestMethod(DisplayName = "验证 null text 抛出 ArgumentNullException")]
    public async Task ProcessIncrementalTextAsync_NullText_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act
        var act = () => pipeline.ProcessIncrementalTextAsync(null!, context);

        // Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(act);
    }

    [TestMethod(DisplayName = "验证 null context 抛出 ArgumentNullException")]
    public async Task ProcessIncrementalTextAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act
        var act = () => pipeline.ProcessIncrementalTextAsync("text", null!);

        // Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(act);
    }

    [TestMethod(DisplayName = "验证多个片段正确合并到 DOM")]
    public async Task ProcessIncrementalTextAsync_FragmentMergedIntoDom()
    {
        // Arrange
        var pipeline = CreatePipeline(minRenderInterval: TimeSpan.FromMinutes(1));
        var context = new SlideMlPipelineContext();

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page><Panel Id=\"p1\"><Rect Id=\"r1\"/></Panel></Page>", context);
        await pipeline.ProcessIncrementalTextAsync("<Panel Id=\"p1\"><Rect Id=\"r2\"/></Panel>", context);

        // Assert
        Assert.Contains("r1", pipeline.CurrentMergedXml);
        Assert.Contains("r2", pipeline.CurrentMergedXml);
    }

    [TestMethod(DisplayName = "错误片段自动回滚，不污染 DOM 树")]
    public async Task ProcessIncrementalTextAsync_ErrorFragment_RolledBack()
    {
        // Arrange
        var pipeline = CreatePipeline(minRenderInterval: TimeSpan.FromMinutes(1));
        var context = new SlideMlPipelineContext();

        // 先合并一个成功的片段
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);
        var xmlBeforeError = pipeline.CurrentMergedXml;

        // Act — 合并一个类型冲突的错误片段
        await pipeline.ProcessIncrementalTextAsync("<Panel Id=\"r1\" X=\"0\"/>", context);

        // Assert — 错误片段被回滚，DOM 树保持干净
        Assert.IsNotEmpty(context.Errors, "应产生类型冲突错误");
        Assert.AreEqual(xmlBeforeError, pipeline.CurrentMergedXml, "错误片段应被回滚，DOM 保持出错前状态");
    }

    [TestMethod(DisplayName = "错误片段回滚后，后续正确片段可正常合并")]
    public async Task ProcessIncrementalTextAsync_ErrorRolledBack_SubsequentFragmentMerges()
    {
        // Arrange
        var pipeline = CreatePipeline(minRenderInterval: TimeSpan.FromMinutes(1));
        var context = new SlideMlPipelineContext();

        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);
        await pipeline.ProcessIncrementalTextAsync("<Panel Id=\"r1\" X=\"0\"/>", context); // 类型冲突错误

        // Act — 回滚后继续合并正确片段
        context.Reset();
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r2\" Width=\"200\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "后续正确片段不应产生错误");
        Assert.Contains("r1", pipeline.CurrentMergedXml, "r1 应保留");
        Assert.Contains("r2", pipeline.CurrentMergedXml, "r2 应存在");
    }

    [TestMethod(DisplayName = "同一增量文本中混合正确和错误片段，错误片段被跳过")]
    public async Task ProcessIncrementalTextAsync_MixedFragments_ErrorSkipped()
    {
        // Arrange
        var pipeline = CreatePipeline(minRenderInterval: TimeSpan.FromMinutes(1));
        var context = new SlideMlPipelineContext();

        // Act — 一个增量文本包含两个片段：一个正确一个错误
        // 正确：<Page><Rect Id=\"r1\"/></Page>
        // 错误：<Panel Id=\"r1\"/> (类型冲突)
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Rect Id=\"r1\" Width=\"100\"/></Page><Panel Id=\"r1\" X=\"0\"/>", context);

        // Assert — r1 保留为 Rect 类型，Panel 片段被回滚
        Assert.IsNotEmpty(context.Errors, "应产生类型冲突错误");
        var xml = pipeline.CurrentMergedXml;
        Assert.Contains("<Rect Id=\"r1\"", xml, "r1 应仍为 Rect");
        Assert.DoesNotContain("Panel", xml, "错误的 Panel 片段应被回滚");
    }

    private static SlideStreamingPipeline CreatePipeline(TimeSpan? minRenderInterval = null)
    {
        var renderResult = new SlideMlRenderResult
        {
            InputXml = "<Page/>",
            OutputXml = "<Page/>",
            Warnings = Array.Empty<string>(),
            Errors = Array.Empty<string>(),
            PreviewImage = new FakePreviewImage(),
        };
        var fakePipeline = new FakeRenderPipeline(renderResult);
        var dispatcher = new FakeMainThreadDispatcher();
        var promptProvider = new SlideMlPromptProvider();
        return new SlideStreamingPipeline(promptProvider, fakePipeline, dispatcher, minRenderInterval);
    }
}
