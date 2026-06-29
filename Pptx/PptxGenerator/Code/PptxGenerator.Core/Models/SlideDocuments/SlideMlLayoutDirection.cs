namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// Panel 子元素排列方向。
/// </summary>
public enum SlideMlLayoutDirection
{
    /// <summary>绝对定位（默认行为）。</summary>
    Absolute,
    /// <summary>水平排列，子元素沿 X 轴依次排布。</summary>
    Horizontal,
    /// <summary>垂直排列，子元素沿 Y 轴依次排布。</summary>
    Vertical,
}
