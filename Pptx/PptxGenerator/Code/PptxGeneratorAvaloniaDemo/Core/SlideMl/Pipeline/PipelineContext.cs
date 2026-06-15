using Avalonia.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// SlideML 生成管道的上下文状态，承载一次生成流程的全部中间结果。
/// </summary>
public sealed class PipelineContext
{
    /// <summary>
    /// 用户原始需求文本。
    /// </summary>
    public string UserPrompt { get; init; } = string.Empty;

    /// <summary>
    /// 生成的 SlideML XML（模型原始输出）。
    /// </summary>
    public string? SlideXml { get; set; }

    /// <summary>
    /// 渲染回填后的 XML。
    /// </summary>
    public string? RenderedXml { get; set; }

    /// <summary>
    /// 渲染警告文本。
    /// </summary>
    public string? Warnings { get; set; }

    /// <summary>
    /// 最终渲染预览图。
    /// </summary>
    public Bitmap? PreviewBitmap { get; set; }

    /// <summary>
    /// SlideML 评估结果。
    /// </summary>
    public SlideEvaluationResult? SlideEvaluation { get; set; }

    /// <summary>
    /// 提示词评估结果。
    /// </summary>
    public PromptEvaluationResult? PromptEvaluation { get; set; }

    /// <summary>
    /// 生成阶段是否成功完成。
    /// </summary>
    public bool IsGenerationComplete { get; set; }

    /// <summary>
    /// 快照 <see cref="SlideRenderTool"/> 的当前状态到上下文。
    /// </summary>
    public void SnapshotFromRenderTool(SlideRenderTool renderTool)
    {
        SlideXml = renderTool.LatestSlideXml;
        RenderedXml = renderTool.LatestRenderedXml;
        Warnings = renderTool.LatestWarnings;
        PreviewBitmap = renderTool.LatestPreviewBitmap;
        IsGenerationComplete = !string.IsNullOrWhiteSpace(SlideXml);
    }
}