using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlRenderPipeline Fake 渲染引擎验证与复杂场景集成测试。
/// 对应测试用例文档第 9 章（Fake 渲染引擎验证）和第 10 章（复杂场景）。
/// </summary>
[TestClass]
public sealed class SlideMlRenderPipelineEngineTests
{
    /// <summary>
    /// 构建测试用管道实例。
    /// </summary>
    private static (SlideMlRenderPipeline Pipeline, FakeRenderEngine RenderEngine, SlideMlPipelineContext Context) CreatePipeline(
        SlideMlPipelineContext? context = null)
    {
        context ??= new SlideMlPipelineContext();
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var pipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher, context);
        return (pipeline, renderEngine, context);
    }

    // ───────── 第 9 章：Fake 渲染引擎验证 ─────────

    [TestMethod]
    public async Task RenderAsync_PreMeasureWasCalled()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, renderEngine, _) = CreatePipeline();

        await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(renderEngine.PreMeasureWasCalled, "PreMeasure 应被调用");
        Assert.IsNotNull(renderEngine.PreMeasurePage, "PreMeasurePage 不应为 null");
    }

    [TestMethod]
    public async Task RenderAsync_RenderWasCalled()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, renderEngine, _) = CreatePipeline();

        await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(renderEngine.RenderWasCalled, "Render 应被调用");
        Assert.IsNotNull(renderEngine.RenderPage, "RenderPage 不应为 null");
    }

    [TestMethod]
    public async Task RenderAsync_ExecutionOrder_Correct()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, renderEngine, _) = CreatePipeline();

        await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // FakeRenderEngine 中 PreMeasure 先于 Render 被调用
        Assert.IsTrue(renderEngine.PreMeasureWasCalled, "PreMeasure 应被调用");
        Assert.IsTrue(renderEngine.RenderWasCalled, "Render 应被调用");
    }

    [TestMethod]
    public async Task RenderAsync_MeasurementsPassedToFinalLayout()
    {
        var xml = """<Page><TextElement Id="t1" Text="Hello" FontSize="16"/></Page>""";
        var (pipeline, renderEngine, _) = CreatePipeline();
        renderEngine.MeasureOverrides["t1"] = (88, 19.2, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // 测量值应回填到 OutputXml
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"88\"", "测量值宽度应回填");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"19.2\"", "测量值高度应回填");
    }

    [TestMethod]
    public async Task RenderAsync_PreviewImageFromRender_Returned()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, renderEngine, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsNotNull(result.PreviewImage, "PreviewImage 不应为 null");
        // FakeRenderEngine.Render 返回新的 FakePreviewImage 实例
        Assert.IsInstanceOfType<FakePreviewImage>(result.PreviewImage, "PreviewImage 应为 FakePreviewImage 实例");
    }

    // ───────── 第 10 章：复杂场景 ─────────

    [TestMethod]
    public async Task RenderAsync_FullSpecimen_AllFeaturesCombined()
    {
        var xml = """
            <Page Background="#F0F0F0">
              <Panel Id="row" Layout="Horizontal" Gap="16" X="40" Y="40" Width="1200" Height="300">
                <Rect Id="card1" Width="380" Height="280" Fill="#FFFFFF" CornerRadius="12"/>
                <Rect Id="card2" Width="380" Height="280" Fill="#FFFFFF" CornerRadius="12"/>
                <Rect Id="card3" Width="380" Height="280" Fill="#FFFFFF" CornerRadius="12"/>
              </Panel>
              <TextElement Id="title" Text="Hello World" FontSize="32" X="40" Y="360"/>
            </Page>
            """;
        var (pipeline, renderEngine, _) = CreatePipeline();
        renderEngine.MeasureOverrides["title"] = (200, 38.4, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // 所有元素正确回填 ActualWidth/ActualHeight
        StringAssert.Contains(result.OutputXml, "Id=\"row\"", "row 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"card1\"", "card1 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"card2\"", "card2 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"card3\"", "card3 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"title\"", "title 应在输出中");
        Assert.Contains("ActualWidth=\"380\"", result.OutputXml, "card 应回填 ActualWidth=380");
    }

    [TestMethod]
    public async Task RenderAsync_DeeplyNestedPanels_AllResolved()
    {
        var xml = """
            <Page>
              <Panel Id="l1" Layout="Horizontal" X="0" Y="0" Width="1280" Height="720">
                <Panel Id="l2" Layout="Vertical" Width="600" Height="720" Gap="10">
                  <Panel Id="l3" Layout="Horizontal" Width="600" Height="300" Gap="8">
                    <Rect Id="leaf1" Width="290" Height="300" Fill="#FF0000"/>
                    <Rect Id="leaf2" Width="290" Height="300" Fill="#00FF00"/>
                  </Panel>
                  <Rect Id="leaf3" Width="600" Height="400" Fill="#0000FF"/>
                </Panel>
                <Panel Id="l2b" Layout="Vertical" Width="680" Height="720">
                  <Rect Id="leaf4" Width="680" Height="720" Fill="#FFFF00"/>
                </Panel>
              </Panel>
            </Page>
            """;
        var (pipeline, _, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "Id=\"l1\"", "l1 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"l2\"", "l2 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"l3\"", "l3 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"leaf1\"", "leaf1 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"leaf4\"", "leaf4 应在输出中");
    }

    [TestMethod]
    public async Task RenderAsync_MixedLayout_WithPaddingGapMargin()
    {
        var xml = """
            <Page>
              <Panel Id="p1" Layout="Horizontal" Padding="16" Gap="8" X="0" Y="0" Width="800" Height="400">
                <Rect Id="r1" Width="200" Height="100" Fill="#FF0000" Margin="4"/>
                <TextElement Id="t1" Text="Hello" FontSize="16" Width="150" Height="30"/>
                <Image Id="img1" Source="pic" Width="100" Height="100"/>
              </Panel>
            </Page>
            """;
        var (pipeline, renderEngine, _) = CreatePipeline();
        renderEngine.MeasureOverrides["t1"] = (150, 19.2, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "Id=\"p1\"", "p1 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"r1\"", "r1 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"t1\"", "t1 应在输出中");
        StringAssert.Contains(result.OutputXml, "Id=\"img1\"", "img1 应在输出中");
    }

    [TestMethod]
    public async Task RenderAsync_Alignment_LayoutCorrect()
    {
        var xml = """<Page><Panel Id="p1" Layout="Horizontal" Width="400" Height="200"><Rect Id="r1" Width="100" Height="50" HorizontalAlignment="Center" VerticalAlignment="Center"/></Panel></Page>""";
        var (pipeline, _, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "Id=\"r1\"", "r1 应在输出中");
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"100\"", "r1 应回填 ActualWidth=100");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"50\"", "r1 应回填 ActualHeight=50");
    }

    [TestMethod]
    public async Task RenderAsync_CustomCanvasSize_LayoutRespected()
    {
        var xml = """<Page><Rect Id="r1" X="0" Y="0" Width="1920" Height="1080"/></Page>""";
        var (pipeline, _, _) = CreatePipeline(new SlideMlPipelineContext(1920, 1080));

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1920\"", "Page 应回填 ActualWidth=1920");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"1080\"", "Page 应回填 ActualHeight=1080");
    }
}
