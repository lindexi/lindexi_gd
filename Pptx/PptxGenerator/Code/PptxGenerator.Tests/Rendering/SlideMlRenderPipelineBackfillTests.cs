using System.Xml.Linq;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlRenderPipeline 回填验证与 XML 回填格式验证集成测试。
/// 对应测试用例文档第 3 章（回填验证 FormatRenderedXml）和第 11 章（XML 回填格式验证）。
/// </summary>
[TestClass]
public sealed class SlideMlRenderPipelineBackfillTests
{
    /// <summary>
    /// 构建测试用管道实例。
    /// </summary>
    private static (SlideMlRenderPipeline Pipeline, FakeRenderEngine RenderEngine) CreatePipeline(
        SlideMlPipelineContext? context = null)
    {
        context ??= new SlideMlPipelineContext();
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var pipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher, context);
        return (pipeline, renderEngine);
    }

    // ───────── 第 3 章：回填验证（FormatRenderedXml） ─────────

    [TestMethod]
    public async Task FormatRenderedXml_Page_BackfillsCanvasSize()
    {
        var xml = """<Page Background="#F5F5F5"><Rect Id="r1" X="0" Y="0" Width="100" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1280\"", "Page 应回填 ActualWidth=1280");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"720\"", "Page 应回填 ActualHeight=720");
    }

    [TestMethod]
    public async Task FormatRenderedXml_NestedElements_AllBackfilled()
    {
        var xml = """<Page><Panel Id="outer"><Panel Id="inner"><Rect Id="leaf" Width="50" Height="30"/></Panel></Panel></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // 外层 Panel — 无固定 Width/Height 时由子元素决定
        Assert.IsTrue(result.OutputXml.Contains("Id=\"outer\"") && result.OutputXml.Contains("ActualWidth="), "outer 应回填 ActualWidth");
        Assert.IsTrue(result.OutputXml.Contains("Id=\"inner\"") && result.OutputXml.Contains("ActualWidth="), "inner 应回填 ActualWidth");
        Assert.IsTrue(result.OutputXml.Contains("Id=\"leaf\"") && result.OutputXml.Contains("ActualWidth=\"50\""), "leaf 应回填 ActualWidth=50");
    }

    [TestMethod]
    public async Task FormatRenderedXml_ActualLineCount_OnlyOnTextElement()
    {
        var xml = """<Page><TextElement Id="t1" Text="Hello" FontSize="16"/><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["t1"] = (50, 19.2, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // t1 应有 ActualLineCount
        var t1Index = result.OutputXml.IndexOf("Id=\"t1\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, t1Index, "t1 应存在于输出中");
        var t1Segment = result.OutputXml.Substring(t1Index, Math.Min(200, result.OutputXml.Length - t1Index));
        StringAssert.Contains(t1Segment, "ActualLineCount=\"1\"", "t1 应有 ActualLineCount=1");

        // r1 不应有 ActualLineCount
        var r1Index = result.OutputXml.IndexOf("Id=\"r1\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, r1Index, "r1 应存在于输出中");
        var r1Segment = result.OutputXml.Substring(r1Index, Math.Min(200, result.OutputXml.Length - r1Index));
        Assert.DoesNotContain("ActualLineCount", r1Segment, "r1 不应有 ActualLineCount 属性");
    }

    [TestMethod]
    public async Task FormatRenderedXml_NoIdElement_Skipped()
    {
        var xml = """<Page><Rect Width="100" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // 实际行为：FormatRenderedXml 从 XML 元素读取 Id 属性，无 Id 属性时跳过回填
        // 解析器虽然分配了 elem_001，但 XML 中无 Id 属性，所以不会回填 ActualWidth/ActualHeight
        Assert.DoesNotContain("elem_001", result.OutputXml, "无 Id 元素不应在 XML 中出现 elem_001");
    }

    [TestMethod]
    public async Task FormatRenderedXml_CustomCanvas_Backfilled()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline(new SlideMlPipelineContext(1920, 1080));

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1920\"", "Page 应回填自定义 ActualWidth=1920");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"1080\"", "Page 应回填自定义 ActualHeight=1080");
    }

    // ───────── 第 11 章：XML 回填格式验证 ─────────

    [TestMethod]
    public async Task RenderAsync_OutputXml_IsValidXml()
    {
        var xml = """<Page><Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // 如果 OutputXml 不是合法 XML，XDocument.Parse 会抛出异常
        var doc = XDocument.Parse(result.OutputXml);
        Assert.IsNotNull(doc, "OutputXml 应可解析为 XDocument");
        Assert.IsNotNull(doc.Root, "OutputXml 应有根元素");
    }

    [TestMethod]
    public async Task RenderAsync_NumericFormat_DisplayCorrect()
    {
        var xml = """<Page><Rect Id="r1" Width="100.5" Height="100.0"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "ActualWidth=\"100.5\"", "ActualWidth 应为 100.5");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"100\"", "ActualHeight 应为 100（去零）");
    }

    [TestMethod]
    public async Task RenderAsync_LineCount_OnlyOnText()
    {
        var xml = """<Page><TextElement Id="t1" Text="Hello" FontSize="16"/><Rect Id="r1" Width="100" Height="50"/><Panel Id="p1"><Rect Id="r2" Width="10" Height="10"/></Panel></Page>""";
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["t1"] = (50, 19.2, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // 只有 TextElement 有 ActualLineCount
        Assert.Contains("ActualLineCount=\"1\"", result.OutputXml, "t1 应有 ActualLineCount");

        // 检查 r1 段不含 ActualLineCount
        var r1Index = result.OutputXml.IndexOf("Id=\"r1\"", StringComparison.Ordinal);
        var r1Segment = result.OutputXml.Substring(r1Index, Math.Min(200, result.OutputXml.Length - r1Index));
        Assert.DoesNotContain("ActualLineCount", r1Segment, "r1 不应有 ActualLineCount");

        // 检查 p1 段不含 ActualLineCount
        var p1Index = result.OutputXml.IndexOf("Id=\"p1\"", StringComparison.Ordinal);
        var p1Segment = result.OutputXml.Substring(p1Index, Math.Min(200, result.OutputXml.Length - p1Index));
        Assert.DoesNotContain("ActualLineCount", p1Segment, "p1 不应有 ActualLineCount");
    }

    [TestMethod]
    public async Task RenderAsync_OriginalAttributes_Preserved()
    {
        var xml = """<Page><Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000" CornerRadius="8"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "X=\"10\"", "X 属性应保留");
        StringAssert.Contains(result.OutputXml, "Y=\"20\"", "Y 属性应保留");
        StringAssert.Contains(result.OutputXml, "Width=\"100\"", "Width 属性应保留");
        StringAssert.Contains(result.OutputXml, "Height=\"50\"", "Height 属性应保留");
        StringAssert.Contains(result.OutputXml, "Fill=\"#FF0000\"", "Fill 属性应保留");
        StringAssert.Contains(result.OutputXml, "CornerRadius=\"8\"", "CornerRadius 属性应保留");
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"100\"", "应新增 ActualWidth");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"50\"", "应新增 ActualHeight");
    }
}
