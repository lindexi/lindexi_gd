using System.Xml.Linq;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

[TestClass]
public sealed class SlideMlIntegrationTests
{
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

    private static string ExtractElementSegment(string xml, string id)
    {
        var index = xml.IndexOf($"Id=\"{id}\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, index, $"元素 Id=\"{id}\" 应存在于输出 XML 中");
        return xml.Substring(index, Math.Min(300, xml.Length - index));
    }

    [TestMethod]
    public async Task HorizontalFlow_ThreeRects_ChildrenArrangedAndBackfilled()
    {
        var xml = """
            <Page Background="#FFFFFF">
              <Panel Id="row" Layout="Horizontal" Gap="12" X="80" Y="260" Width="1120">
                <Rect Id="card1" Width="340" Height="260" Fill="#FFFFFF" />
                <Rect Id="card2" Width="340" Height="260" Fill="#FFFFFF" />
                <Rect Id="card3" Width="340" Height="260" Fill="#FFFFFF" />
              </Panel>
            </Page>
            """;
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        StringAssert.Contains(result.OutputXml, "RenderSize=\"1280x720\"", "Page 应回填 RenderSize");

        var rowSegment = ExtractElementSegment(result.OutputXml, "row");
        StringAssert.Contains(rowSegment, "RenderSize=\"1120x260\"", "Panel#row RenderSize");

        var card1Segment = ExtractElementSegment(result.OutputXml, "card1");
        StringAssert.Contains(card1Segment, "RenderSize=\"340x260\"", "card1 RenderSize");
        StringAssert.Contains(card1Segment, "RenderLocation=\"80x260\"", "card1 RenderLocation");

        var card2Segment = ExtractElementSegment(result.OutputXml, "card2");
        StringAssert.Contains(card2Segment, "RenderSize=\"340x260\"", "card2 RenderSize");
        StringAssert.Contains(card2Segment, "RenderLocation=\"432x260\"", "card2 RenderLocation");

        var card3Segment = ExtractElementSegment(result.OutputXml, "card3");
        StringAssert.Contains(card3Segment, "RenderSize=\"340x260\"", "card3 RenderSize");
        StringAssert.Contains(card3Segment, "RenderLocation=\"784x260\"", "card3 RenderLocation");

        Assert.IsEmpty(result.Warnings, "Warnings 应为空");
        Assert.IsEmpty(result.Errors, "Errors 应为空");
    }

    [TestMethod]
    public async Task VerticalFlow_WithPadding_ChildrenOffsetAndMeasured()
    {
        var xml = """
            <Page>
              <Panel Id="col" Layout="Vertical" Gap="16" Padding="24" X="100" Y="100" Width="400">
                <TextElement Id="title" Text="标题" FontSize="24" IsBold="True" />
                <TextElement Id="desc" Text="正文内容" FontSize="15" Width="352" />
              </Panel>
            </Page>
            """;
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["title"] = (48, 28.8, 1);
        renderEngine.MeasureOverrides["desc"] = (352, 18, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        var titleSegment = ExtractElementSegment(result.OutputXml, "title");
        StringAssert.Contains(titleSegment, "RenderSize=\"48x28.8\"", "title RenderSize");

        var descSegment = ExtractElementSegment(result.OutputXml, "desc");
        StringAssert.Contains(descSegment, "RenderSize=\"352x18\"", "desc RenderSize");

        var colSegment = ExtractElementSegment(result.OutputXml, "col");
        StringAssert.Contains(colSegment, "RenderSize=\"400x110.8\"", "Panel#col RenderSize");

        Assert.IsEmpty(result.Warnings, "Warnings 应为空");
    }

    [TestMethod]
    public async Task AbsolutePanel_NestedTextElements_CoordinatesCorrect()
    {
        var xml = """
            <Page Background="#F5F5F5">
              <Panel Id="hero" X="0" Y="0" Width="1280" Height="360" Background="#1A1A2E">
                <TextElement Id="title" X="80" Y="120" Width="1120"
                             Text="SlideML V2" FontSize="56" IsBold="True" Foreground="#FFFFFF" />
                <TextElement Id="sub" X="80" Y="200" Width="1120"
                             Text="副标题" FontSize="24" Foreground="#CCCCDD" />
              </Panel>
            </Page>
            """;
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["title"] = (1120, 67.2, 1);
        renderEngine.MeasureOverrides["sub"] = (1120, 28.8, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        var heroSegment = ExtractElementSegment(result.OutputXml, "hero");
        StringAssert.Contains(heroSegment, "RenderSize=\"1280x360\"", "Panel#hero RenderSize");

        var titleSegment = ExtractElementSegment(result.OutputXml, "title");
        StringAssert.Contains(titleSegment, "RenderSize=\"1120x67.2\"", "title RenderSize");

        var subSegment = ExtractElementSegment(result.OutputXml, "sub");
        StringAssert.Contains(subSegment, "RenderSize=\"1120x28.8\"", "sub RenderSize");

        Assert.IsEmpty(result.Warnings, "Warnings 应为空");
    }

    [TestMethod]
    public async Task TextOverflow_HeightExceeded_WarningProduced()
    {
        var xml = """
            <Page>
              <TextElement Id="long-text" X="40" Y="40" Width="400" Height="30"
                           Text="这是一段很长的文本内容，会超出容器高度限制"
                           FontSize="16" />
            </Page>
            """;
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["long-text"] = (400, 80, 5);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("long-text") && w.Contains("ActualLineCount") && w.Contains("超出容器高度")),
            "应包含文本溢出容器高度的 Warning");

        var segment = ExtractElementSegment(result.OutputXml, "long-text");
        StringAssert.Contains(segment, "ActualLineCount=\"5\"", "long-text 应回填 ActualLineCount=5");
    }

    [TestMethod]
    public async Task FlowLayoutOverflow_WidthExceeded_WarningProduced()
    {
        var xml = """
            <Page>
              <Panel Id="row" Layout="Horizontal" Gap="8" X="0" Y="0" Width="200">
                <Rect Id="r1" Width="150" Height="50" />
                <Rect Id="r2" Width="150" Height="50" />
              </Panel>
            </Page>
            """;
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("row") && w.Contains("流式布局内容宽度") && w.Contains("超出 Panel 宽度")),
            "应包含流式布局溢出 Panel 宽度的 Warning");

        var r1Segment = ExtractElementSegment(result.OutputXml, "r1");
        StringAssert.Contains(r1Segment, "RenderSize=\"150x50\"", "r1 应回填 RenderSize");

        var r2Segment = ExtractElementSegment(result.OutputXml, "r2");
        StringAssert.Contains(r2Segment, "RenderSize=\"150x50\"", "r2 应回填 RenderSize");
    }

    [TestMethod]
    public async Task ElementOutsideCanvas_RightAndBottomBoundary_WarningsProduced()
    {
        var xml = """
            <Page>
              <Rect Id="big" X="1200" Y="600" Width="200" Height="200" Fill="#FF0000" />
            </Page>
            """;
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("big") && w.Contains("右边界") && w.Contains("1400") && w.Contains("1280")),
            "应包含右边界超出画布宽度的 Warning");

        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("big") && w.Contains("下边界") && w.Contains("800") && w.Contains("720")),
            "应包含下边界超出画布高度的 Warning");
    }

    [TestMethod]
    public async Task ParseError_NonPageRoot_ErrorPreviewReturned()
    {
        var xml = "<NotPage></NotPage>";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(
            result.Errors.Any(e => e.Contains("parser") && e.Contains("SlideML 解析失败")),
            "应包含解析失败 Error");

        Assert.IsNotNull(result.PreviewImage, "应返回错误预览图");
        Assert.AreEqual(xml, result.OutputXml, "OutputXml 应原样返回输入 XML");
    }

    [TestMethod]
    public async Task FullSpecExample_GradientCardsSpans_AllParsedAndBackfilled()
    {
        var xml = """
            <Page Background="#F5F5F5">
              <Panel Id="hero" X="0" Y="0" Width="1280" Height="360">
                <Fill>
                  <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
                    <Stop Offset="0" Color="#1A1A2E"/>
                    <Stop Offset="1" Color="#4A4A6E"/>
                  </LinearGradient>
                </Fill>
                <TextElement Id="hero-title" X="80" Y="120" Width="1120"
                             Text="SlideML V2" FontSize="56" IsBold="True"
                             Foreground="#FFFFFF" TextAlignment="Center" />
                <TextElement Id="hero-sub" X="80" Y="200" Width="1120"
                             Text="让大语言模型生成专业幻灯片"
                             FontSize="24" Foreground="#CCCCDD" TextAlignment="Center" />
              </Panel>
              <Panel Id="cards-row" Layout="Horizontal" Gap="24" X="80" Y="400" Width="1120" Height="280">
                <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
                      Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
                <TextElement Id="card1-title" X="24" Y="24" Width="292"
                             Text="流式布局" FontSize="22" IsBold="True" Foreground="#333" />
                <TextElement Id="card1-desc" X="24" Y="72" Width="292"
                             Text="支持 Panel Layout='Horizontal'/'Vertical'，自动排列子元素。"
                             FontSize="15" Foreground="#666" />
                <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
                      Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
                <TextElement Id="card2-title" X="24" Y="24" Width="292"
                             Text="渐变与阴影" FontSize="22" IsBold="True" Foreground="#333" />
                <TextElement Id="card2-desc" X="24" Y="72" Width="292"
                             Text="支持线性渐变填充/描边和元素阴影效果。"
                             FontSize="15" Foreground="#666" />
                <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
                      Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
                <TextElement Id="card3-title" X="24" Y="24" Width="292"
                             Text="富文本" FontSize="22" IsBold="True" Foreground="#333" />
                <TextElement Id="card3-desc" X="24" Y="72" Width="292">
                  <Span Text="支持 Span 子元素" FontSize="15" Foreground="#666"/>
                  <Span Text="在同一文本块内" FontSize="15" IsBold="True" Foreground="#333"/>
                  <Span Text="混排多种样式。" FontSize="15" Foreground="#666"/>
                </TextElement>
              </Panel>
            </Page>
            """;
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsEmpty(result.Errors, "不应有解析错误");

        StringAssert.Contains(result.OutputXml, "RenderSize=\"1280x720\"", "Page 应回填 RenderSize");

        var heroSegment = ExtractElementSegment(result.OutputXml, "hero");
        StringAssert.Contains(heroSegment, "RenderSize=\"1280x360\"", "Panel#hero RenderSize");

        var cardsRowSegment = ExtractElementSegment(result.OutputXml, "cards-row");
        StringAssert.Contains(cardsRowSegment, "RenderSize=\"1120x280\"", "Panel#cards-row RenderSize");

        foreach (var id in new[] { "hero-title", "hero-sub", "card1-title", "card1-desc",
                                   "card2-title", "card2-desc", "card3-title", "card3-desc" })
        {
            var segment = ExtractElementSegment(result.OutputXml, id);
            StringAssert.Contains(segment, "RenderSize=", $"{id} 应回填 RenderSize");
        }

        StringAssert.Contains(result.OutputXml, "LinearGradient", "渐变背景应被解析");
        StringAssert.Contains(result.OutputXml, "<Span", "Span 子元素应被解析");

        var doc = XDocument.Parse(result.OutputXml);
        Assert.IsNotNull(doc.Root, "OutputXml 应有根元素");
    }
}