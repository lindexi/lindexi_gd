using System.Collections.Generic;

namespace PptxGenerator;

/// <summary>
/// 渲染流水线共享上下文，包含画布尺寸等全局配置以及警告收集。
/// </summary>
public sealed class SlidePipelineContext
{
    /// <summary>
    /// 默认画布宽度。
    /// </summary>
    public const int DefaultCanvasWidth = 1280;

    /// <summary>
    /// 默认画布高度。
    /// </summary>
    public const int DefaultCanvasHeight = 720;

    /// <summary>
    /// 初始化 <see cref="SlidePipelineContext"/> 的新实例。
    /// </summary>
    public SlidePipelineContext()
    {
        CanvasWidth = DefaultCanvasWidth;
        CanvasHeight = DefaultCanvasHeight;
    }

    /// <summary>
    /// 初始化 <see cref="SlidePipelineContext"/> 的新实例并指定画布尺寸。
    /// </summary>
    /// <param name="canvasWidth">画布宽度。</param>
    /// <param name="canvasHeight">画布高度。</param>
    public SlidePipelineContext(int canvasWidth, int canvasHeight)
    {
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
    }

    /// <summary>
    /// 画布宽度（像素）。
    /// </summary>
    public int CanvasWidth { get; }

    /// <summary>
    /// 画布高度（像素）。
    /// </summary>
    public int CanvasHeight { get; }

    /// <summary>
    /// 渲染过程中的警告信息收集列表。
    /// </summary>
    public List<string> Warnings { get; } = new();
}
