using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlRenderPipeline Warning 收集集成测试。
/// 对应测试用例文档第 6 章（Warning 收集）。
/// </summary>
[TestClass]
public sealed class SlideMlRenderPipelineWarningTests
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

    [TestMethod]
    public async Task RenderAsync_ElementOutsideCanvas_WarningCollected()
    {
        var xml = """<Page><Rect Id="r1" X="1200" Y="600" Width="200" Height="200"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(result.Warnings.Count > 0, "应包含 Warning");
        var hasRightOverflow = result.Warnings.Any(w => w.Contains("右边界") && w.Contains("1400") && w.Contains("1280"));
        Assert.IsTrue(hasRightOverflow, "应包含右边界超出画布宽度的警告");
        var hasBottomOverflow = result.Warnings.Any(w => w.Contains("下边界") && w.Contains("800") && w.Contains("720"));
        Assert.IsTrue(hasBottomOverflow, "应包含下边界超出画布高度的警告");
    }

    [TestMethod]
    public async Task RenderAsync_FlowLayoutOverflow_WarningCollected()
    {
        var xml = """<Page><Panel Id="p1" Layout="Horizontal" Width="150" Gap="8"><Rect Id="r1" Width="100" Height="50"/><Rect Id="r2" Width="100" Height="50"/></Panel></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(result.Warnings.Count > 0, "应包含 Warning");
        var hasOverflow = result.Warnings.Any(w => w.Contains("流式布局内容宽度") && w.Contains("超出 Panel 宽度") && w.Contains("150"));
        Assert.IsTrue(hasOverflow, "应包含流式布局溢出警告");
    }

    [TestMethod]
    public async Task RenderAsync_TextHeightOverflow_WarningCollected()
    {
        var xml = """<Page><TextElement Id="t1" Text="Long text..." Width="400" Height="30" FontSize="16"/></Page>""";
        var (pipeline, renderEngine) = CreatePipeline();
        renderEngine.MeasureOverrides["t1"] = (400, 80, 5);

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(result.Warnings.Count > 0, "应包含 Warning");
        var hasTextOverflow = result.Warnings.Any(w => w.Contains("超出容器高度"));
        Assert.IsTrue(hasTextOverflow, "应包含文本溢出容器高度的警告");
    }

    [TestMethod]
    public async Task RenderAsync_UnknownAttribute_WarningCollected()
    {
        var xml = """<Page><Rect Id="r1" Unknown="value" Width="100" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(result.Warnings.Count > 0, "应包含 Warning");
        var hasUnknownAttr = result.Warnings.Any(w => w.Contains("未知属性") && w.Contains("Unknown"));
        Assert.IsTrue(hasUnknownAttr, "应包含未知属性 \"Unknown\" 的警告");
    }

    [TestMethod]
    public async Task RenderAsync_UnknownTag_WarningCollected()
    {
        var xml = """<Page><UnknownTag/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(result.Warnings.Count > 0, "应包含 Warning");
        var hasUnknownTag = result.Warnings.Any(w => w.Contains("未知标签") && w.Contains("UnknownTag"));
        Assert.IsTrue(hasUnknownTag, "应包含未知标签 \"UnknownTag\" 的警告");
    }

    [TestMethod]
    public async Task RenderAsync_MultipleWarnings_AllCollected()
    {
        // 同时含超出边界 + 未知属性
        var xml = """<Page><Rect Id="r1" Unknown="value" X="1200" Y="600" Width="200" Height="200"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsTrue(result.Warnings.Count >= 3, "应至少有 3 个 Warning（未知属性 + 右边界 + 下边界）");
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("未知属性")), "应包含未知属性警告");
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("右边界")), "应包含右边界警告");
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("下边界")), "应包含下边界警告");
    }
}
