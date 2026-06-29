namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 线性渐变画刷，起止点坐标范围为 0~1（相对于元素边界）。
/// </summary>
public sealed class SlideMlLinearGradientBrush : ISlideMlBrush
{
    /// <summary>
    /// 渐变起点 X 坐标。
    /// </summary>
    public double X1 { get; init; }

    /// <summary>
    /// 渐变起点 Y 坐标。
    /// </summary>
    public double Y1 { get; init; }

    /// <summary>
    /// 渐变终点 X 坐标。
    /// </summary>
    public double X2 { get; init; } = 1;

    /// <summary>
    /// 渐变终点 Y 坐标。
    /// </summary>
    public double Y2 { get; init; }

    /// <summary>
    /// 渐变停止点列表。
    /// </summary>
    public IReadOnlyList<SlideMlGradientStop> Stops { get; init; } = Array.Empty<SlideMlGradientStop>();
}