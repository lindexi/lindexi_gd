using System.Text.Json.Serialization;

namespace PptxGenerator.Models;

/// <summary>
/// 文档级上下文，承载画布尺寸等不可变的全局配置。
/// 可独立传递到评估器、优化器等需要画布信息的组件。
/// </summary>
public sealed class SlideDocumentContext
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
    /// 初始化 <see cref="SlideDocumentContext"/> 的新实例，使用默认画布尺寸 1280×720。
    /// </summary>
    public SlideDocumentContext()
    {
        CanvasWidth = DefaultCanvasWidth;
        CanvasHeight = DefaultCanvasHeight;
    }

    /// <summary>
    /// 初始化 <see cref="SlideDocumentContext"/> 的新实例并指定画布尺寸。
    /// </summary>
    /// <param name="canvasWidth">画布宽度（像素）。</param>
    /// <param name="canvasHeight">画布高度（像素）。</param>
    [JsonConstructor]
    public SlideDocumentContext(int canvasWidth, int canvasHeight)
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
