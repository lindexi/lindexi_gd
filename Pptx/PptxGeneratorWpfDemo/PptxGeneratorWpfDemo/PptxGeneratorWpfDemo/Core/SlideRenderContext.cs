namespace PptxGenerator;

/// <summary>
/// 渲染流水线共享上下文，包含画布尺寸等全局配置。
/// </summary>
public sealed class SlideRenderContext
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
    /// 初始化 <see cref="SlideRenderContext"/> 的新实例。
    /// </summary>
    public SlideRenderContext()
    {
        CanvasWidth = DefaultCanvasWidth;
        CanvasHeight = DefaultCanvasHeight;
    }

    /// <summary>
    /// 初始化 <see cref="SlideRenderContext"/> 的新实例并指定画布尺寸。
    /// </summary>
    /// <param name="canvasWidth">画布宽度。</param>
    /// <param name="canvasHeight">画布高度。</param>
    public SlideRenderContext(int canvasWidth, int canvasHeight)
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
}
