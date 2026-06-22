namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 纯色画刷。
/// </summary>
public sealed class SlideMlSolidColorBrush : ISlideMlBrush
{
    /// <summary>
    /// 颜色字符串（如 "#FF0000"）。
    /// </summary>
    public string Color { get; init; } = string.Empty;
}