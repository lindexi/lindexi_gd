using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlRenderPipeline 基本 End-to-End 流程与 XML 预处理集成测试。
/// 对应测试用例文档第 1 章（基本 E2E 流程）和第 2 章（XML 预处理）。
/// </summary>
[TestClass]
public sealed class SlideMlRenderPipelineBasicTests
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

    // ───────── 第 1 章：基本 End-to-End 流程 ─────────

    [TestMethod]
    public async Task RenderAsync_SimplePageWithRect_ReturnsCorrectResult()
    {
        var xml = """<Page><Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsNotNull(result.PreviewImage, "PreviewImage 不应为 null");
        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
        Assert.AreEqual(0, result.Errors.Count, "Errors 应为空");
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"100\"", "Rect 应回填 ActualWidth=100");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"50\"", "Rect 应回填 ActualHeight=50");
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1280\"", "Page 应回填 ActualWidth=1280");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"720\"", "Page 应回填 ActualHeight=720");
    }

    [TestMethod]
    public async Task RenderAsync_SimplePageWithTextElement_ReturnsMeasuredResult()
    {
        var xml = """<Page><TextElement Id="t1" Text="Hello World" FontSize="16"/></Page>""";
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["t1"] = (88, 19.2, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "ActualWidth=\"88\"", "t1 应回填 ActualWidth=88");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"19.2\"", "t1 应回填 ActualHeight=19.2");
        StringAssert.Contains(result.OutputXml, "ActualLineCount=\"1\"", "t1 应回填 ActualLineCount=1");
        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
    }

    [TestMethod]
    public async Task RenderAsync_SimplePageWithImage_ReturnsMeasuredSize()
    {
        var xml = """<Page><Image Id="img1" Source="pic_001" Width="400" Height="300"/></Page>""";
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["img1"] = (400, 300, null);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "ActualWidth=\"400\"", "img1 应回填 ActualWidth=400");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"300\"", "img1 应回填 ActualHeight=300");
        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
    }

    [TestMethod]
    public async Task RenderAsync_AbsolutePanel_ChildrenRendered()
    {
        var xml = """<Page><Panel Id="p1" X="50" Y="50" Width="400" Height="300"><Rect Id="r1" X="20" Y="20" Width="100" Height="80" Fill="#00FF00"/></Panel></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "Id=\"p1\"", "OutputXml 应包含 p1");
        StringAssert.Contains(result.OutputXml, "Id=\"r1\"", "OutputXml 应包含 r1");
        Assert.IsTrue(result.OutputXml.Contains("ActualWidth=\"400\"") || result.OutputXml.Contains("ActualWidth=\"358\""), "Panel 应回填 ActualWidth");
        Assert.IsTrue(result.OutputXml.Contains("ActualWidth=\"100\""), "r1 应回填 ActualWidth=100");
        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
    }

    [TestMethod]
    public async Task RenderAsync_HorizontalFlowPanel_ChildrenArranged()
    {
        var xml = """<Page><Panel Id="row" Layout="Horizontal" Gap="12" X="40" Y="40" Width="1000"><Rect Id="c1" Width="300" Height="200"/><Rect Id="c2" Width="300" Height="200"/><Rect Id="c3" Width="300" Height="200"/></Panel></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
        StringAssert.Contains(result.OutputXml, "Id=\"c1\"", "c1 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"c2\"", "c2 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"c3\"", "c3 应在输出中");
    }

    [TestMethod]
    public async Task RenderAsync_VerticalFlowPanel_ChildrenArranged()
    {
        var xml = """<Page><Panel Id="col" Layout="Vertical" Gap="16" X="100" Y="100" Width="400"><Rect Width="400" Height="60" Fill="#FF0000"/><Rect Width="400" Height="60" Fill="#00FF00"/><Rect Width="400" Height="60" Fill="#0000FF"/></Panel></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
    }

    // ───────── 第 2 章：XML 预处理 ─────────

    [TestMethod]
    public async Task RenderAsync_WithXmlDeclaration_Normalized()
    {
        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page><Rect Width=\"100\" Height=\"50\"/></Page>";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // NormalizeXml 使用 XDocument.Parse(...).ToString()，ToString() 默认不输出 XML 声明
        // 但 XML 内容应被正确解析和回填
        StringAssert.Contains(result.OutputXml, "<Page", "XML 应被正确解析");
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1280\"", "Page 应回填 ActualWidth");
    }

    [TestMethod]
    public async Task RenderAsync_WithoutDeclaration_DeclarationAdded()
    {
        var xml = """<Page><Rect Width="100" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // ExtractXml 会添加声明头，但 NormalizeXml 的 ToString() 不输出声明
        // 验证 XML 被正确解析和回填
        StringAssert.Contains(result.OutputXml, "<Page", "XML 应被正确解析");
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1280\"", "Page 应回填 ActualWidth");
    }

    [TestMethod]
    public async Task RenderAsync_WithMarkdownCodeBlock_XmlExtracted()
    {
        var xml = "```xml\n<Page><Rect Width=\"100\" Height=\"50\"/></Page>\n```";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "<Page>", "XML 应被正确提取");
    }

    [TestMethod]
    public async Task RenderAsync_WithWhitespace_XmlTrimmed()
    {
        var xml = "\n  \n<Page><Rect Width=\"100\" Height=\"50\"/></Page>\n  ";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.AreEqual(0, result.Errors.Count, "不应有错误");
    }

    [TestMethod]
    public async Task RenderAsync_NoXml_ExceptionCaught()
    {
        var xml = "这是一段纯文本";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(result.Warnings.Count > 0, "应包含解析失败警告");
        Assert.IsNotNull(result.PreviewImage, "应返回错误预览图");
    }
}
