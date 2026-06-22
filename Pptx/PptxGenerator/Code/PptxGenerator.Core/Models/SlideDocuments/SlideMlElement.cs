namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// SlideML 元素的抽象基类，包含所有元素的公共属性。
/// UI 框架（WPF/Avalonia）相关的缓存属性由渲染引擎自行维护。
/// </summary>
public abstract class SlideMlElement
{
    /// <summary>
    /// 元素唯一标识符。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 元素水平位置。
    /// </summary>
    public double? X { get; init; }

    /// <summary>
    /// 元素垂直位置。
    /// </summary>
    public double? Y { get; init; }

    /// <summary>
    /// 元素宽度。
    /// </summary>
    public double? Width { get; init; }

    /// <summary>
    /// 元素高度。
    /// </summary>
    public double? Height { get; init; }

    /// <summary>
    /// 水平对齐方式。
    /// </summary>
    public SlideMlHorizontalAlignment? HorizontalAlignment { get; init; }

    /// <summary>
    /// 垂直对齐方式。
    /// </summary>
    public SlideMlVerticalAlignment? VerticalAlignment { get; init; }

    /// <summary>
    /// 元素透明度（0~1）。
    /// </summary>
    public double Opacity { get; init; } = 1;

    /// <summary>
    /// 元素外边距。在流式布局中影响元素间距，在绝对定位中影响坐标偏移。
    /// </summary>
    public SlideMlThickness? Margin { get; init; }

    /// <summary>
    /// 元素在自身坐标系中的局部边界。
    /// </summary>
    public SlideMlRect LocalBounds { get; set; }

    /// <summary>
    /// 元素在父容器中的布局边界。
    /// </summary>
    public SlideMlRect LayoutBounds { get; set; }

    /// <summary>
    /// 元素实际渲染宽度。
    /// </summary>
    public double ActualWidth { get; set; }

    /// <summary>
    /// 元素实际渲染高度。
    /// </summary>
    public double ActualHeight { get; set; }
}
