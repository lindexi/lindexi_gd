using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;
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
        var renderedResults = new List<SlideMlRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\"/></Page>", context);
        await pipeline.ProcessStreamEndAsync(context);

        // Assert：ProcessIncrementalTextAsync 触发 1 次实时渲染，ProcessStreamEndAsync 触发 1 次最终渲染
        Assert.HasCount(2, renderedResults);
    }

    [TestMethod(DisplayName = "验证流每次片段合并后触发即时渲染")]
    public async Task ProcessIncrementalTextAsync_RealTimeRender_TriggeredAfterEachFragment()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideMlRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\"/></Page>", context);

        // Assert：片段合并后立即触发渲染（无需等待 ProcessStreamEndAsync）
        Assert.HasCount(1, renderedResults);
    }

    [TestMethod(DisplayName = "首片段为悬空样式元素：暂存样式，后续 Page 引用后形成可渲染页面")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentDanglingStyleElement_ThenPageCanReferenceStyle()
    {
        // Arrange
        var pipeline = CreatePipelineWithRealRenderEngine();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideMlRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act — 首个片段不是 Page，而是仅用于样式复用的悬空元素
        await pipeline.ProcessIncrementalTextAsync(
            "<Rect Id=\"card-template\" StyleId=\"card-style\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>",
            context);
        var xmlAfterDanglingFragment = pipeline.CurrentMergedXml;
        var errorsAfterDanglingFragment = context.Errors.ToArray();
        context.Reset();

        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Rect Id=\"card1\" StyleFrom=\"card-style\" X=\"10\" Y=\"20\"/></Page>",
            context);

        // Assert — 当前实现会先保留悬空样式状态；实时渲染层会报告首片段尚不是 Page
        Assert.IsNotEmpty(errorsAfterDanglingFragment, "首个悬空样式片段尚未形成 Page，实时渲染层会记录当前状态不可渲染");
        Assert.IsEmpty(context.Errors, "后续 Page 引用悬空样式时不应产生新的错误");
        Assert.Contains("card-template", xmlAfterDanglingFragment, "Page 到来前，管道内部合并状态会暂存悬空样式元素");

        var doc = System.Xml.Linq.XDocument.Parse(pipeline.CurrentMergedXml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName, "后续 Page 应成为最终可渲染根元素");
        Assert.IsFalse(
            doc.Root.Elements().Any(e => e.Attribute("Id")?.Value == "card-template"),
            "悬空样式元素不应进入 Page 子树参与渲染");

        var card1 = doc.Root.Elements("Rect").Single(e => e.Attribute("Id")?.Value == "card1");
        Assert.AreEqual("#FF0000", card1.Attribute("Fill")?.Value, "Page 内元素应继承悬空样式 Fill");
        Assert.AreEqual("100", card1.Attribute("Width")?.Value, "Page 内元素应继承悬空样式 Width");
        Assert.AreEqual("50", card1.Attribute("Height")?.Value, "Page 内元素应继承悬空样式 Height");
        Assert.IsTrue(renderedResults.Count >= 2, "首个悬空样式片段和后续 Page 片段都会触发管道渲染事件以暴露当前状态");
    }

    [TestMethod(DisplayName = "验证最终渲染触发")]
    public async Task ProcessStreamEndAsync_FinalRender_Triggers()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideMlRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act：先触发一次渲染
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\"/></Page>", context);
        var countAfterFirst = renderedResults.Count;

        // 最终渲染必定触发
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
        var pipeline = CreatePipeline();
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
        var pipeline = CreatePipeline();
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
        var pipeline = CreatePipeline();
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
        var pipeline = CreatePipeline();
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

    // ───────── 边界行为：首个片段即出错 ─────────

    [TestMethod(DisplayName = "首个片段即出错：回滚后 DOM 为空字符串")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentError_RollsBackToEmpty()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段就是错误片段（未知元素）
        await pipeline.ProcessIncrementalTextAsync("<UnknownElement Id=\"x\"/>", context);

        // Assert — 错误被收集，DOM 回滚到初始空状态
        Assert.IsNotEmpty(context.Errors, "应产生未知元素错误");
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml),
            "首个片段出错后回滚，DOM 应为空字符串");
    }

    [TestMethod(DisplayName = "首个片段即 XML 格式错误：回滚后 DOM 为空字符串")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentXmlFormatError_RollsBackToEmpty()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段是格式错误的 XML（未闭合标签）
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\"></Page>", context);

        // Assert
        Assert.IsNotEmpty(context.Errors, "应产生 XML 格式错误");
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml),
            "首个片段 XML 格式错误回滚后，DOM 应为空字符串");
    }

    [TestMethod(DisplayName = "首个片段即出错：回滚后后续正确片段可正常合并")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentError_ThenValidFragment_MergesSuccessfully()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段出错
        await pipeline.ProcessIncrementalTextAsync("<UnknownElement Id=\"x\"/>", context);
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml),
            "首个片段出错后 DOM 应为空");

        // 重置诊断信息后继续输入正确片段
        context.Reset();
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>", context);

        // Assert — 后续正确片段成功合并，DOM 从空状态正常构建
        Assert.IsEmpty(context.Errors, "后续正确片段不应产生错误");
        var xml = pipeline.CurrentMergedXml;
        Assert.Contains("r1", xml, "r1 应存在");
        Assert.Contains("Page", xml, "Page 根元素应存在");
    }

    [TestMethod(DisplayName = "首个片段即出错：重试模拟场景中合并器从空状态恢复")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentError_RetryScenario_RecoversFromEmpty()
    {
        // Arrange — 模拟 StreamingSlideGenerator.GenerateAsync 的重试流程
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // 第一轮：首个片段出错
        await pipeline.ProcessIncrementalTextAsync("<UnknownElement Id=\"bad\"/>", context);
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml),
            "第一轮首个片段出错后 DOM 应为空");

        // 模拟重试前的重置：Context.Reset + Pipeline.ResetExtractor
        context.Reset();
        pipeline.ResetExtractor();

        // Act — 第二轮：有效片段
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);

        // Assert — 从空状态成功恢复
        Assert.IsEmpty(context.Errors, "第二轮不应有错误");
        Assert.Contains("r1", pipeline.CurrentMergedXml, "r1 应存在");
    }

    [TestMethod(DisplayName = "首个片段即出错：连续两次出错后第三次成功")]
    public async Task ProcessIncrementalTextAsync_FirstTwoFragmentsError_ThirdSucceeds()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // 第一次出错
        await pipeline.ProcessIncrementalTextAsync("<UnknownElement Id=\"bad1\"/>", context);
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml), "第一次出错后 DOM 应为空");

        // 模拟重试
        context.Reset();
        pipeline.ResetExtractor();

        // 第二次仍然出错
        await pipeline.ProcessIncrementalTextAsync("<AnotherBadElement Id=\"bad2\"/>", context);
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml), "第二次出错后 DOM 应仍为空");

        // 再次重试
        context.Reset();
        pipeline.ResetExtractor();

        // Act — 第三次有效
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "第三次不应有错误");
        Assert.Contains("r1", pipeline.CurrentMergedXml, "第三次有效片段应成功合并");
    }

    [TestMethod(DisplayName = "同一增量中首个片段出错、后续片段正确：错误片段回滚后正确片段仍可合并")]
    public async Task ProcessIncrementalTextAsync_FirstErrorThenValidInSameBatch_BothProcessed()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 同一批次中：错误片段在前，正确片段在后
        // 注意：ProcessIncrementalTextAsync 内部对每个片段单独检查错误，
        // 出错的片段会被回滚但不会中断后续片段处理
        await pipeline.ProcessIncrementalTextAsync(
            "<UnknownElement Id=\"bad\"/><Page><Rect Id=\"r1\" Width=\"100\"/></Page>",
            context);

        // Assert — 错误被收集，但正确的 Page 片段仍然成功合并
        Assert.IsNotEmpty(context.Errors, "应产生未知元素错误");
        Assert.Contains("r1", pipeline.CurrentMergedXml, "正确片段应成功合并");
        Assert.Contains("Page", pipeline.CurrentMergedXml, "Page 根元素应存在");
    }

    [TestMethod(DisplayName = "首个片段出错后 ProcessStreamEnd：不触发渲染且 DOM 为空")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentError_ThenStreamEnd_EmptyAndNoRender()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedCount = 0;
        pipeline.Rendered += _ => renderedCount++;

        // Act — 首个片段出错，然后流结束
        await pipeline.ProcessIncrementalTextAsync("<UnknownElement Id=\"bad\"/>", context);
        await pipeline.ProcessStreamEndAsync(context);

        // Assert — DOM 为空，无渲染
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml), "DOM 应为空");
        Assert.AreEqual(0, renderedCount, "空 DOM 不应触发渲染");
    }

    [TestMethod(DisplayName = "首个片段同片段内 Id 类型冲突出错：回滚后 DOM 为空，后续可正常合并")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentDuplicateIdTypeConflict_RollsBackToEmpty()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段内 Panel 和 Rect 共用 Id="dup"（类型不同），触发错误
        // 整个片段（含 Page）被回滚到初始空状态
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Panel Id=\"dup\"><Rect Id=\"dup\"/></Panel></Page>", context);

        // Assert
        Assert.IsNotEmpty(context.Errors, "应产生同片段内 Id 类型冲突错误");
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml),
            "首个片段因 Id 类型冲突出错回滚后，DOM 应为空（Page 也被回滚）");

        // 后续正确片段仍可合并
        context.Reset();
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);
        Assert.IsEmpty(context.Errors, "后续正确片段不应出错");
        Assert.Contains("r1", pipeline.CurrentMergedXml, "后续正确片段应成功合并");
    }

    // ───────── 渲染层属性格式错误（Padding="abc"）─────────

    [TestMethod(DisplayName = "Panel Padding 格式错误：合并层无错，渲染层产生错误")]
    public async Task ProcessIncrementalTextAsync_PanelInvalidPadding_MergerOk_RenderError()
    {
        // Arrange — 使用真实渲染管道，使 Padding="abc" 在渲染阶段被检测出来
        var pipeline = CreatePipelineWithRealRenderEngine();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideMlRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act — Panel 的 Padding 值为 "abc"（非数值），合并层不报错（XML 结构合法、Id 存在）
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Panel Id=\"p1\" Padding=\"abc\"><Rect Id=\"r1\" Width=\"100\"/></Panel></Page>",
            context);

        // Assert — 渲染层错误已同步写入 context.Errors
        Assert.IsNotEmpty(context.Errors, "渲染层错误应同步写入 context.Errors");
        Assert.IsTrue(
            context.Errors.Any(e => e.Contains("Padding") && e.Contains("abc")),
            "context.Errors 应包含 Padding 格式错误");

        // 渲染层产生错误（SlideMlParser.GetOptionalDouble 检测到非数值）
        Assert.IsNotEmpty(renderedResults, "应触发渲染");
        Assert.IsNotEmpty(renderedResults[^1].Errors, "渲染结果应包含 Padding 格式错误");
        Assert.IsTrue(
            renderedResults[^1].Errors.Any(e => e.Contains("Padding") && e.Contains("abc")),
            "错误信息应包含 Padding 和 abc");
    }

    [TestMethod(DisplayName = "Panel Padding 格式错误：合并器 DOM 保留 Panel，渲染结果含错误")]
    public async Task ProcessIncrementalTextAsync_PanelInvalidPadding_DomRetained_RenderHasError()
    {
        // Arrange
        var pipeline = CreatePipelineWithRealRenderEngine();
        var context = new SlideMlPipelineContext();

        // Act
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Panel Id=\"p1\" Padding=\"abc\"><Rect Id=\"r1\" Width=\"100\"/></Panel></Page>",
            context);

        // Assert — 合并器 DOM 保留了 Panel（合并层不关心属性值格式）
        var xml = pipeline.CurrentMergedXml;
        Assert.Contains("Padding=\"abc\"", xml, "合并器 DOM 应保留原始 Padding=\"abc\"");
        Assert.Contains("p1", xml, "Panel p1 应保留");
        Assert.Contains("r1", xml, "Rect r1 应保留");
    }

    [TestMethod(DisplayName = "Panel Padding 格式错误后修正：后续合并覆盖 Padding 为正确值，渲染无错")]
    public async Task ProcessIncrementalTextAsync_PanelInvalidPadding_FixedBySubsequentMerge()
    {
        // Arrange
        var pipeline = CreatePipelineWithRealRenderEngine();
        var context = new SlideMlPipelineContext();

        // 第一轮：Padding 格式错误
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Panel Id=\"p1\" Padding=\"abc\"><Rect Id=\"r1\" Width=\"100\"/></Panel></Page>",
            context);
        Assert.Contains("Padding=\"abc\"", pipeline.CurrentMergedXml);

        // Act — 模拟重试修正：用正确 Padding 覆盖
        context.Reset();
        pipeline.ResetExtractor();
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Panel Id=\"p1\" Padding=\"16\"/></Page>",
            context);

        // Assert — DOM 中 Padding 被覆盖为 16
        Assert.IsEmpty(context.Errors, "合并层不应有错误");
        var xml = pipeline.CurrentMergedXml;
        Assert.Contains("Padding=\"16\"", xml, "Padding 应被覆盖为 16");
        Assert.DoesNotContain("Padding=\"abc\"", xml, "旧的 Padding=\"abc\" 应被覆盖");
    }

    [TestMethod(DisplayName = "Panel Padding 格式错误：ProcessStreamEnd 渲染仍产生错误")]
    public async Task ProcessIncrementalTextAsync_PanelInvalidPadding_StreamEndRenderHasError()
    {
        // Arrange
        var pipeline = CreatePipelineWithRealRenderEngine();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideMlRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act — 片段合并后即时渲染检测到错误，ProcessStreamEnd 再次渲染
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Panel Id=\"p1\" Padding=\"xyz\"/></Page>",
            context);
        await pipeline.ProcessStreamEndAsync(context);

        // Assert — 渲染错误已同步写入 context.Errors
        Assert.IsNotEmpty(context.Errors, "渲染层错误应同步写入 context.Errors");
        Assert.IsTrue(
            context.Errors.Any(e => e.Contains("Padding") && e.Contains("xyz")),
            "context.Errors 应包含 Padding 格式错误");
        // ProcessStreamEnd 仍会触发一次渲染
        Assert.IsNotEmpty(renderedResults, "应触发渲染");
    }

    [TestMethod(DisplayName = "首个片段 Panel Padding 格式错误：合并成功，渲染层产生错误")]
    public async Task ProcessIncrementalTextAsync_FirstFragmentPanelInvalidPadding_MergerOk_RenderError()
    {
        // Arrange — 首个片段就是含格式错误 Padding 的 Page
        var pipeline = CreatePipelineWithRealRenderEngine();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideMlRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        await pipeline.ProcessIncrementalTextAsync(
            "<Page><Panel Id=\"p1\" Padding=\"abc\"><Rect Id=\"r1\" Width=\"100\"/></Panel></Page>",
            context);

        // Assert — 渲染层错误已同步写入 context.Errors（ProcessIncrementalTextAsync 同步返回）
        Assert.IsNotEmpty(context.Errors, "渲染层错误应同步写入 context.Errors");
        Assert.IsTrue(
            context.Errors.Any(e => e.Contains("Padding") && e.Contains("abc")),
            "context.Errors 应包含 Padding 格式错误");
        Assert.Contains("p1", pipeline.CurrentMergedXml, "DOM 应包含 Panel");

        // 渲染层检测出错误
        Assert.IsNotEmpty(renderedResults, "应触发渲染");
        Assert.IsTrue(
            renderedResults[^1].Errors.Any(e => e.Contains("Padding") && e.Contains("abc")),
            "渲染结果应包含 Padding 格式错误");
    }

    private static SlideStreamingPipeline CreatePipeline()
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
        return new SlideStreamingPipeline(promptProvider, fakePipeline, dispatcher);
    }

    /// <summary>
    /// 使用真实渲染管道（含 SlideMlParser + FakeRenderEngine）创建管道，
    /// 使属性值格式错误（如 Padding="abc"）在渲染阶段被检测出来。
    /// </summary>
    private static SlideStreamingPipeline CreatePipelineWithRealRenderEngine()
    {
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var renderPipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher);
        var promptProvider = new SlideMlPromptProvider();
        return new SlideStreamingPipeline(promptProvider, renderPipeline, dispatcher);
    }
}
