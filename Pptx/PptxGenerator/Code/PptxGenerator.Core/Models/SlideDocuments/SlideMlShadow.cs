using System.Globalization;

namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 元素阴影效果。
/// </summary>
public sealed class SlideMlShadow
{
    /// <summary>
    /// 阴影水平偏移。
    /// </summary>
    public double OffsetX { get; set; }

    /// <summary>
    /// 阴影垂直偏移。
    /// </summary>
    public double OffsetY { get; set; } = 4;

    /// <summary>
    /// 阴影模糊半径。
    /// </summary>
    public double Blur { get; set; } = 12;

    /// <summary>
    /// 阴影颜色字符串。
    /// </summary>
    public string Color { get; set; } = "#00000033";

    /// <summary>
    /// 阴影不透明度。
    /// </summary>
    public double Opacity { get; set; } = 1;

    /// <summary>
    /// 从属性字符串解析，如 "0 4 12 #00000033"。
    /// </summary>
    public static SlideMlShadow? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var shadow = new SlideMlShadow();
        if (parts.Length > 0 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var ox))
        {
            shadow.OffsetX = ox;
        }

        if (parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var oy))
        {
            shadow.OffsetY = oy;
        }

        if (parts.Length > 2 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var blur))
        {
            shadow.Blur = blur;
        }

        if (parts.Length > 3)
        {
            shadow.Color = parts[3];
        }

        return shadow;
    }
}
