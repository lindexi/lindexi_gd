namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 渐变停止点（0~1 位置 + 颜色）。
/// </summary>
public sealed class SlideMlGradientStop
{
    /// <summary>
    /// 渐变位置（0~1）。
    /// </summary>
    public double Offset { get; init; }

    /// <summary>
    /// 颜色字符串（如 "#FF0000"）。
    /// </summary>
    public string Color { get; init; } = string.Empty;
}