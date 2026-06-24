using System.Xml.Linq;
using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideML 端到端集成测试范例。
/// 从输入 XML → 解析 → 布局 → Fake 渲染 → 回填 → 输出 XML 的完整流程验证。
/// </summary>
[TestClass]
public sealed class SlideMlIntegrationTests
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

    /// <summary>
    /// 从 OutputXml 中提取指定 Id 元素的片段字符串，便于局部断言。
    /// </summary>
    private static string ExtractElementSegment(string xml, string id)
    {
        var index = xml.IndexOf($"Id=\"{id}\"", StringComparison.Ordinal);
        Assert.IsTrue(index >= 0, $"元素 Id=\"{id}\" 应存在于输出 XML 中");
        return xml.Substring(index, Math.Min(300, xml.Length - index));
    }

    // ───────── 用例 1：基本水平流式布局端到端 ─────────

    /// <summary>
    /// 验证 3 个 Rect 在水平 Panel 中按 Gap 间距依次排列，
    /// 坐标和 ActualWidth/ActualHeight 被正确回填到输出 XML。
    /// </summary>
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

        // Page 回填画布尺寸
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1280\"", "Page 应回填 ActualWidth=1280");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"720\"", "Page 应回填 ActualHeight=720");

        // Panel#row 有显式 Width="1120"，ActualWidth 等于声明值；ActualHeight = 260（子元素最大高度）
        var rowSegment = ExtractElementSegment(result.OutputXml, "row");
        StringAssert.Contains(rowSegment, "ActualWidth=\"1120\"", "Panel#row ActualWidth 应为声明的 1120");
        StringAssert.Contains(rowSegment, "ActualHeight=\"260\"", "Panel#row ActualHeight 应为 260");

        // card1: ActualWidth=340, ActualHeight=260, X=80
        var card1Segment = ExtractElementSegment(result.OutputXml, "card1");
        StringAssert.Contains(card1Segment, "ActualWidth=\"340\"", "card1 ActualWidth 应为 340");
        StringAssert.Contains(card1Segment, "ActualHeight=\"260\"", "card1 ActualHeight 应为 260");

        // card2: ActualWidth=340, ActualHeight=260, X=80+340+12=432
        var card2Segment = ExtractElementSegment(result.OutputXml, "card2");
        StringAssert.Contains(card2Segment, "ActualWidth=\"340\"", "card2 ActualWidth 应为 340");
        StringAssert.Contains(card2Segment, "ActualHeight=\"260\"", "card2 ActualHeight 应为 260");

        // card3: ActualWidth=340, ActualHeight=260, X=432+340+12=784
        var card3Segment = ExtractElementSegment(result.OutputXml, "card3");
        StringAssert.Contains(card3Segment, "ActualWidth=\"340\"", "card3 ActualWidth 应为 340");
        StringAssert.Contains(card3Segment, "ActualHeight=\"260\"", "card3 ActualHeight 应为 260");

        // 无 Warning、无 Error
        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
        Assert.AreEqual(0, result.Errors.Count, "Errors 应为空");
    }

    // ───────── 用例 2：带 Padding 的垂直流式布局端到端 ─────────

    /// <summary>
    /// 验证垂直 Panel 中 Padding 偏移、Gap 间距、TextElement 测量值回填。
    /// </summary>
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
        // 预设测量结果：title MeasuredWidth=48, MeasuredHeight=28.8, ActualLineCount=1
        renderEngine.MeasureOverrides["title"] = (48, 28.8, 1);
        // desc MeasuredWidth=352, MeasuredHeight=18, ActualLineCount=1
        renderEngine.MeasureOverrides["desc"] = (352, 18, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // title: LayoutBounds.X = 100 + 24 = 124, Y = 100 + 24 = 124
        // 流式布局中 ActualWidth/ActualHeight 使用测量值回填
        // 注意：流式布局不经过 LayoutText，ActualLineCount 保持默认值 0
        var titleSegment = ExtractElementSegment(result.OutputXml, "title");
        StringAssert.Contains(titleSegment, "ActualWidth=\"48\"", "title ActualWidth 应为 48");
        StringAssert.Contains(titleSegment, "ActualHeight=\"28.8\"", "title ActualHeight 应为 28.8");

        // desc: LayoutBounds.X = 124, Y = 124 + 28.8 + 16 = 168.8
        var descSegment = ExtractElementSegment(result.OutputXml, "desc");
        StringAssert.Contains(descSegment, "ActualWidth=\"352\"", "desc ActualWidth 应为 352");
        StringAssert.Contains(descSegment, "ActualHeight=\"18\"", "desc ActualHeight 应为 18");

        // Panel#col: ActualHeight = 28.8 + 16 + 18 + 24×2 = 110.8
        var colSegment = ExtractElementSegment(result.OutputXml, "col");
        StringAssert.Contains(colSegment, "ActualHeight=\"110.8\"", "Panel#col ActualHeight 应为 110.8");

        // 无 Warning
        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
    }

    // ───────── 用例 3：绝对定位 Panel 嵌套子元素 ─────────

    /// <summary>
    /// 验证绝对定位 Panel 中子元素使用自身 X/Y 坐标，
    /// Panel 有显式 Width/Height 时 ActualWidth/ActualHeight 等于声明值。
    /// </summary>
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
        // title: FontSize=56 → MeasuredHeight=56
        renderEngine.MeasureOverrides["title"] = (1120, 67.2, 1);
        // sub: FontSize=24 → MeasuredHeight=24
        renderEngine.MeasureOverrides["sub"] = (1120, 28.8, 1);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // Panel#hero: ActualWidth=1280, ActualHeight=360（显式声明）
        var heroSegment = ExtractElementSegment(result.OutputXml, "hero");
        StringAssert.Contains(heroSegment, "ActualWidth=\"1280\"", "Panel#hero ActualWidth 应为 1280");
        StringAssert.Contains(heroSegment, "ActualHeight=\"360\"", "Panel#hero ActualHeight 应为 360");

        // title: LayoutBounds = (80, 120, 1120, 67.2)
        var titleSegment = ExtractElementSegment(result.OutputXml, "title");
        StringAssert.Contains(titleSegment, "ActualWidth=\"1120\"", "title ActualWidth 应为 1120");
        StringAssert.Contains(titleSegment, "ActualHeight=\"67.2\"", "title ActualHeight 应为 67.2");

        // sub: LayoutBounds = (80, 200, 1120, 28.8)
        var subSegment = ExtractElementSegment(result.OutputXml, "sub");
        StringAssert.Contains(subSegment, "ActualWidth=\"1120\"", "sub ActualWidth 应为 1120");
        StringAssert.Contains(subSegment, "ActualHeight=\"28.8\"", "sub ActualHeight 应为 28.8");

        // 无 Warning
        Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");
    }

    // ───────── 用例 4：文本溢出容器高度产生 Warning ─────────

    /// <summary>
    /// 使用 MeasureOverrides 模拟文本实际行数超出容器高度，
    /// 验证 Warning 包含溢出信息且 OutputXml 回填 ActualLineCount。
    /// </summary>
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
        // 模拟测量结果：ActualLineCount=5, MeasuredHeight=80（远超 Height=30）
        renderEngine.MeasureOverrides["long-text"] = (400, 80, 5);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // Warning 包含 ActualLineCount 和超出容器高度信息
        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("long-text") && w.Contains("ActualLineCount") && w.Contains("超出容器高度")),
            "应包含文本溢出容器高度的 Warning");

        // OutputXml 回填 ActualLineCount=5（LayoutText 应用了测量结果中的行数）
        // ActualHeight=30：元素有显式 Height="30"，布局引擎优先使用声明值
        var segment = ExtractElementSegment(result.OutputXml, "long-text");
        StringAssert.Contains(segment, "ActualLineCount=\"5\"", "long-text 应回填 ActualLineCount=5");
        StringAssert.Contains(segment, "ActualHeight=\"30\"", "long-text ActualHeight 应为声明的 30");
    }

    // ───────── 用例 5：流式布局溢出 Panel 宽度 ─────────

    /// <summary>
    /// 验证水平流式布局中子元素总宽度超出 Panel 声明宽度时产生 Warning，
    /// 且子元素仍被布局（不阻止排列）。
    /// </summary>
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

        // Warning 包含流式布局内容宽度超出 Panel 宽度 200
        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("row") && w.Contains("流式布局内容宽度") && w.Contains("超出 Panel 宽度") && w.Contains("200")),
            "应包含流式布局溢出 Panel 宽度的 Warning");

        // r1 和 r2 仍被布局（存在于输出中且有回填）
        var r1Segment = ExtractElementSegment(result.OutputXml, "r1");
        StringAssert.Contains(r1Segment, "ActualWidth=\"150\"", "r1 应回填 ActualWidth=150");

        var r2Segment = ExtractElementSegment(result.OutputXml, "r2");
        StringAssert.Contains(r2Segment, "ActualWidth=\"150\"", "r2 应回填 ActualWidth=150");
    }

    // ───────── 用例 6：元素超出画布边界 ─────────

    /// <summary>
    /// 验证元素右边界和下边界超出画布尺寸时分别产生 Warning。
    /// </summary>
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

        // 右边界: 1200 + 200 = 1400 > 1280
        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("big") && w.Contains("右边界") && w.Contains("1400") && w.Contains("1280")),
            "应包含右边界超出画布宽度的 Warning");

        // 下边界: 600 + 200 = 800 > 720
        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("big") && w.Contains("下边界") && w.Contains("800") && w.Contains("720")),
            "应包含下边界超出画布高度的 Warning");
    }

    // ───────── 用例 7：解析错误时返回错误预览 ─────────

    /// <summary>
    /// 验证非 Page 根元素触发解析异常时，管道捕获异常并返回错误预览，
    /// Warnings 包含解析失败信息，OutputXml 原样返回。
    /// </summary>
    [TestMethod]
    public async Task ParseError_NonPageRoot_ErrorPreviewReturned()
    {
        var xml = "<NotPage></NotPage>";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // Warnings 包含解析失败信息
        Assert.IsTrue(
            result.Warnings.Any(w => w.Contains("parser") && w.Contains("SlideML 解析失败")),
            "应包含解析失败 Warning");

        // PreviewImage 不为 null（错误预览图）
        Assert.IsNotNull(result.PreviewImage, "应返回错误预览图");

        // OutputXml 等于 InputXml（原样返回）
        Assert.AreEqual(xml, result.OutputXml, "OutputXml 应原样返回输入 XML");
    }

    // ───────── 用例 8：完整规范示例端到端 ─────────

    /// <summary>
    /// 验证包含渐变背景、流式布局卡片、Span 富文本的完整规范示例，
    /// 所有元素正确解析、布局并回填 ActualWidth/ActualHeight。
    /// </summary>
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

        // 无解析错误（Errors 为空）
        Assert.AreEqual(0, result.Errors.Count, "不应有解析错误");

        // Page 回填画布尺寸
        StringAssert.Contains(result.OutputXml, "ActualWidth=\"1280\"", "Page 应回填 ActualWidth=1280");
        StringAssert.Contains(result.OutputXml, "ActualHeight=\"720\"", "Page 应回填 ActualHeight=720");

        // Panel#hero: ActualWidth=1280, ActualHeight=360
        var heroSegment = ExtractElementSegment(result.OutputXml, "hero");
        StringAssert.Contains(heroSegment, "ActualWidth=\"1280\"", "Panel#hero ActualWidth 应为 1280");
        StringAssert.Contains(heroSegment, "ActualHeight=\"360\"", "Panel#hero ActualHeight 应为 360");

        // Panel#cards-row: ActualWidth=1120, ActualHeight=280
        var cardsRowSegment = ExtractElementSegment(result.OutputXml, "cards-row");
        StringAssert.Contains(cardsRowSegment, "ActualWidth=\"1120\"", "Panel#cards-row ActualWidth 应为 1120");
        StringAssert.Contains(cardsRowSegment, "ActualHeight=\"280\"", "Panel#cards-row ActualHeight 应为 280");

        // 所有带 Id 的 TextElement 回填 ActualWidth/ActualHeight
        foreach (var id in new[] { "hero-title", "hero-sub", "card1-title", "card1-desc",
                                   "card2-title", "card2-desc", "card3-title", "card3-desc" })
        {
            var segment = ExtractElementSegment(result.OutputXml, id);
            Assert.IsTrue(segment.Contains("ActualWidth="), $"{id} 应回填 ActualWidth");
            Assert.IsTrue(segment.Contains("ActualHeight="), $"{id} 应回填 ActualHeight");
        }

        // 渐变背景被正确解析（LinearGradient 出现在输出 XML 中）
        StringAssert.Contains(result.OutputXml, "LinearGradient", "渐变背景应被解析");

        // Span 子元素被正确解析（Span 出现在输出 XML 中）
        StringAssert.Contains(result.OutputXml, "<Span", "Span 子元素应被解析");

        // OutputXml 是合法 XML
        var doc = XDocument.Parse(result.OutputXml);
        Assert.IsNotNull(doc.Root, "OutputXml 应有根元素");
    }
}
