namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 矩形元素。
/// </summary>
public sealed class SlideMlRectElement : SlideMlElement
{
    /// <summary>
    /// 填充画刷（纯色或渐变）。
    /// </summary>
    public ISlideMlBrush? Fill { get; init; }

    /// <summary>
    /// 描边画刷（纯色或渐变）。
    /// </summary>
    public ISlideMlBrush? Stroke { get; init; }

    /// <summary>
    /// 描边宽度。
    /// </summary>
    public double StrokeThickness { get; init; }

    /// <summary>
    /// 圆角半径。支持单值（统一圆角）或 <see cref="SlideMlCornerRadius"/>（四角独立）。
    /// </summary>
    public SlideMlCornerRadius? CornerRadius { get; init; }

    /// <summary>
    /// 元素阴影效果。
    /// </summary>
    public SlideMlShadow? Shadow { get; init; }

    /// <summary>
    /// 阴影的原始字符串表示，用于 XML 回填。
    /// </summary>
    public string? ShadowString { get; init; }

    /// <summary>
    /// 虚线描边模式（逗号分隔的数值列表）。
    /// </summary>
    public IReadOnlyList<double>? StrokeDashArray { get; init; }
}
