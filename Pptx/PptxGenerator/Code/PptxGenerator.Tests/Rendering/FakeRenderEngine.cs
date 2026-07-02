using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// 测试用渲染引擎 Fake 实现，记录调用状态并提供可配置的测量结果。
/// </summary>
public sealed class FakeRenderEngine : ISlideMlRenderEngine
{
    /// <summary>
    /// PreMeasure 是否被调用。
    /// </summary>
    public bool PreMeasureWasCalled { get; private set; }

    /// <summary>
    /// Render 是否被调用。
    /// </summary>
    public bool RenderWasCalled { get; private set; }

    /// <summary>
    /// PreMeasure 接收到的页面。
    /// </summary>
    public SlideMlPage? PreMeasurePage { get; private set; }

    /// <summary>
    /// Render 接收到的页面。
    /// </summary>
    public SlideMlPage? RenderPage { get; private set; }

    /// <summary>
    /// 预设测量结果，可由测试设置以覆盖默认计算逻辑。
    /// </summary>
    public Dictionary<string, (double Width, double Height, int? LineCount)> MeasureOverrides { get; set; } = new();

    /// <inheritdoc />
    public SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
    {
        PreMeasureWasCalled = true;
        PreMeasurePage = page;
        var measurements = new Dictionary<string, SlideMlElementMeasureResult>();
        FillMeasurements(page.Children, measurements);
        return new SlideMlElementMeasurements(measurements);
    }

    /// <inheritdoc />
    public IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context)
    {
        RenderWasCalled = true;
        RenderPage = page;
        return new FakePreviewImage();
    }

    /// <inheritdoc />
    public IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context)
        => new FakePreviewImage();

    private void FillMeasurements(List<SlideMlElement> children, Dictionary<string, SlideMlElementMeasureResult> dict)
    {
        foreach (var child in children)
        {
            if (MeasureOverrides.TryGetValue(child.Id, out var ov))
            {
                dict[child.Id] = new SlideMlElementMeasureResult
                {
                    MeasuredWidth = ov.Width,
                    MeasuredHeight = ov.Height,
                    ActualLineCount = ov.LineCount,
                };
            }
            else
            {
                dict[child.Id] = ComputeDefaultMeasure(child);
            }

            if (child is SlideMlPanelElement panel)
            {
                FillMeasurements(panel.Children, dict);
            }
        }
    }

    private static SlideMlElementMeasureResult ComputeDefaultMeasure(SlideMlElement element)
    {
        return element switch
        {
            SlideMlTextElement text => new()
            {
                MeasuredWidth = text.Width ?? text.Text.Length * text.FontSize * 0.6,
                MeasuredHeight = text.Height ?? text.FontSize,
                ActualLineCount = 1,
            },
            SlideMlImageElement img => new()
            {
                MeasuredWidth = img.Width ?? 240,
                MeasuredHeight = img.Height ?? 180,
            },
            _ => new()
            {
                MeasuredWidth = element.Width ?? 0,
                MeasuredHeight = element.Height ?? 0,
            },
        };
    }
}
