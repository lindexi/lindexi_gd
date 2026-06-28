using PptxGenerator.Models;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 流式渲染集成测试，模拟 LLM 逐 token 输出 SlideML XML 的端到端场景。
/// 使用真实的 SlideMlRenderPipeline（配合 FakeRenderEngine + SlideMlLayoutEngine + FakeMainThreadDispatcher）
/// 验证 解析 → 布局 → 渲染 完整流程。
/// </summary>
[TestClass]
public sealed class SlideStreamingIntegrationTests
{
    /// <summary>
    /// 构建测试用管道实例，使用真实渲染管道而非 Fake。
    /// </summary>
    private static (SlideStreamingPipeline Pipeline, SlideMlRenderPipeline RenderPipeline) CreatePipeline()
    {
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var renderPipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher);
        var promptProvider = new SlideMlPromptProvider();
        var pipeline = new SlideStreamingPipeline(promptProvider, renderPipeline, dispatcher);
        return (pipeline, renderPipeline);
    }

    /// <summary>
    /// 将完整 XML 拆分成指定大小的 token。
    /// </summary>
    private static IEnumerable<string> SplitIntoTokens(string text, int tokenSize)
    {
        for (var i = 0; i < text.Length; i += tokenSize)
        {
            var length = Math.Min(tokenSize, text.Length - i);
            yield return text.Substring(i, length);
        }
    }

    // ───────── 用例 1：单页逐 token 流式渲染 ─────────

    /// <summary>
    /// 模拟 LLM 逐 token（每 10 个字符）输出完整页面 XML，验证端到端渲染正确。
    /// </summary>
    [TestMethod]
    public async Task Integration_SinglePageStreamedTokenByToken_RendersCorrectly()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var xml = "<Page Background=\"#FFFFFF\"><TextElement Id=\"title\" Text=\"Hello\" FontSize=\"32\" X=\"10\" Y=\"10\" Width=\"200\"/></Page>";

        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        var streamCompletedValues = new List<string>();
        pipeline.StreamCompleted += streamCompletedValues.Add;

        // Act
        foreach (var token in SplitIntoTokens(xml, 10))
        {
            pipeline.ProcessIncrementalText(token, context);
        }

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, renderedResults.Count, "Rendered 事件应被触发 1 次");
        Assert.AreEqual(1, streamCompletedValues.Count, "StreamCompleted 事件应被触发 1 次");
        StringAssert.Contains(pipeline.CurrentMergedXml, "Page", "合并 XML 应包含 Page");
        StringAssert.Contains(pipeline.CurrentMergedXml, "TextElement", "合并 XML 应包含 TextElement");
        Assert.AreEqual(0, context.Errors.Count, "合并阶段 Errors 应为空");
    }

    // ───────── 用例 2：多片段流式输出 ─────────

    /// <summary>
    /// 模拟 LLM 先输出自闭合 Page，再输出完整 Page（含 Panel 和 Rect），验证多片段合并。
    /// </summary>
    [TestMethod]
    public async Task Integration_MultiFragmentStreamed_AllFragmentsReceived()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var firstXml = "<Page Background=\"#FFFFFF\"/>";
        var secondXml = "<Page Background=\"#F5F5F5\"><Panel Id=\"p1\" X=\"0\" Y=\"0\" Width=\"100\" Height=\"50\"><Rect Id=\"r1\" Fill=\"#FF0000\" Width=\"50\" Height=\"30\"/></Panel></Page>";

        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        foreach (var token in SplitIntoTokens(firstXml, 15))
        {
            pipeline.ProcessIncrementalText(token, context);
        }

        foreach (var token in SplitIntoTokens(secondXml, 15))
        {
            pipeline.ProcessIncrementalText(token, context);
        }

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.IsTrue(fragments.Count >= 2, "FragmentReceived 事件应至少触发 2 次");
        StringAssert.Contains(pipeline.CurrentMergedXml, "Panel", "合并 XML 应包含 Panel");
        StringAssert.Contains(pipeline.CurrentMergedXml, "Rect", "合并 XML 应包含 Rect");
        Assert.AreEqual(1, renderedResults.Count, "Rendered 事件应被触发 1 次");
    }

    // ───────── 用例 3：逐字符输出，标签中间断开 ─────────

    /// <summary>
    /// 模拟 LLM 每次只输出 1 个字符，验证标签中间断开时片段提取器正确合并。
    /// </summary>
    [TestMethod]
    public async Task Integration_TokenSplitMidTag_CorrectlyMerges()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var xml = "<Page><Panel Id=\"header\" X=\"0\" Y=\"0\" Width=\"100\" Height=\"50\"><TextElement Id=\"t1\" Text=\"Hi\" FontSize=\"16\"/></Panel></Page>";

        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act — 每次只输出 1 个字符
        foreach (var token in SplitIntoTokens(xml, 1))
        {
            pipeline.ProcessIncrementalText(token, context);
        }

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        StringAssert.Contains(pipeline.CurrentMergedXml, "header", "合并 XML 应包含 header");
        StringAssert.Contains(pipeline.CurrentMergedXml, "t1", "合并 XML 应包含 t1");
        Assert.AreEqual(1, renderedResults.Count, "Rendered 事件应被触发 1 次");
        Assert.AreEqual(0, context.Errors.Count, "合并阶段 Errors 应为空");
    }

    // ───────── 用例 4：XML 声明后跟 Page ─────────

    /// <summary>
    /// 模拟 LLM 输出 XML 声明后跟 Page，验证处理指令被正确跳过，Page 片段被提取。
    /// </summary>
    [TestMethod]
    public async Task Integration_XmlDeclarationFollowedByPage_CorrectlyParsed()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var xml = "<?xml version=\"1.0\"?><Page><Rect Id=\"r1\" X=\"0\" Y=\"0\" Width=\"100\" Height=\"50\" Fill=\"#0000FF\"/></Page>";

        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        foreach (var token in SplitIntoTokens(xml, 20))
        {
            pipeline.ProcessIncrementalText(token, context);
        }

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.IsTrue(fragments.Count >= 1, "FragmentReceived 事件应至少触发 1 次（Page 片段）");
        StringAssert.Contains(pipeline.CurrentMergedXml, "Page", "合并 XML 应包含 Page");
        StringAssert.Contains(pipeline.CurrentMergedXml, "Rect", "合并 XML 应包含 Rect");
        Assert.AreEqual(1, renderedResults.Count, "Rendered 事件应被触发 1 次");
    }

    // ───────── 用例 5：嵌套 Panel 流式输出 ─────────

    /// <summary>
    /// 模拟 LLM 输出嵌套 Panel 的 XML，验证多层嵌套结构正确合并。
    /// </summary>
    [TestMethod]
    public async Task Integration_NestedPanelsStreamed_CorrectStructure()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var xml = "<Page><Panel Id=\"outer\" X=\"10\" Y=\"10\" Width=\"500\" Height=\"300\"><Panel Id=\"inner\" X=\"20\" Y=\"20\" Width=\"200\" Height=\"100\" Background=\"#FF0000\"><Rect Id=\"r1\" Width=\"50\" Height=\"30\" Fill=\"#00FF00\"/></Panel></Panel></Page>";

        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        foreach (var token in SplitIntoTokens(xml, 30))
        {
            pipeline.ProcessIncrementalText(token, context);
        }

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        StringAssert.Contains(pipeline.CurrentMergedXml, "outer", "合并 XML 应包含 outer");
        StringAssert.Contains(pipeline.CurrentMergedXml, "inner", "合并 XML 应包含 inner");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1", "合并 XML 应包含 r1");
        Assert.AreEqual(1, renderedResults.Count, "Rendered 事件应被触发 1 次");
        Assert.AreEqual(0, context.Errors.Count, "合并阶段 Errors 应为空");
    }

    // ───────── 用例 6：增量合并，第二个片段更新属性 ─────────

    /// <summary>
    /// 模拟 LLM 先输出完整页面，再输出更新属性的片段，验证属性覆盖且保留原属性。
    /// </summary>
    [TestMethod]
    public async Task Integration_IncrementalMerge_SecondFragmentUpdatesAttributes()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var firstXml = "<Page Background=\"#FFFFFF\"><Rect Id=\"r1\" X=\"0\" Y=\"0\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>";
        var secondXml = "<Rect Id=\"r1\" Width=\"200\"/>";

        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        pipeline.ProcessIncrementalText(firstXml, context);
        pipeline.ProcessIncrementalText(secondXml, context);

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        StringAssert.Contains(pipeline.CurrentMergedXml, "Width=\"200\"", "合并 XML 应包含更新后的 Width=\"200\"");
        StringAssert.Contains(pipeline.CurrentMergedXml, "Fill=\"#FF0000\"", "合并 XML 应保留原有的 Fill=\"#FF0000\"");
        Assert.AreEqual(1, renderedResults.Count, "Rendered 事件应被触发 1 次");
    }

    // ───────── 用例 7：Remove 元素流式输出 ─────────

    /// <summary>
    /// 模拟 LLM 先输出包含多个 Rect 的 Panel，再输出 Remove 指令移除其中一个，验证元素被正确移除。
    /// </summary>
    [TestMethod]
    public async Task Integration_RemoveElementStreamed_ElementRemoved()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var firstXml = "<Page><Panel Id=\"p1\"><Rect Id=\"r1\" Fill=\"#FF0000\"/><Rect Id=\"r2\" Fill=\"#00FF00\"/></Panel></Page>";
        var removeXml = "<Remove TargetId=\"r1\"/>";

        // Act
        pipeline.ProcessIncrementalText(firstXml, context);
        pipeline.ProcessIncrementalText(removeXml, context);

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.IsFalse(pipeline.CurrentMergedXml.Contains("r1"), "合并 XML 不应包含已移除的 r1");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r2", "合并 XML 应保留未被移除的 r2");
    }

    // ───────── 用例 8：空流不渲染 ─────────

    /// <summary>
    /// 不输出任何增量文本，直接调用流结束，验证不触发渲染但触发 StreamCompleted。
    /// </summary>
    [TestMethod]
    public async Task Integration_EmptyStream_NoRender()
    {
        // Arrange
        var (pipeline, _) = CreatePipeline();
        var context = new SlideMlPipelineContext();

        var renderedCount = 0;
        pipeline.Rendered += _ => renderedCount++;

        var streamCompletedValues = new List<string>();
        pipeline.StreamCompleted += streamCompletedValues.Add;

        // Act
        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(0, renderedCount, "Rendered 事件不应被触发");
        Assert.AreEqual(1, streamCompletedValues.Count, "StreamCompleted 事件应被触发 1 次");
        Assert.AreEqual(string.Empty, streamCompletedValues[0], "StreamCompleted 参数应为空字符串");
    }
}
