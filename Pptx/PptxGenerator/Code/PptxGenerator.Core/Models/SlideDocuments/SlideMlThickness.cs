using System.Globalization;

namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 四边间距值，用于 Margin 属性。
/// </summary>
public readonly record struct SlideMlThickness
{
    /// <summary>
    /// 左边距。
    /// </summary>
    public double Left { get; init; }

    /// <summary>
    /// 上边距。
    /// </summary>
    public double Top { get; init; }

    /// <summary>
    /// 右边距。
    /// </summary>
    public double Right { get; init; }

    /// <summary>
    /// 下边距。
    /// </summary>
    public double Bottom { get; init; }

    /// <summary>
    /// 从逗号分隔的字符串解析，如 "0,0,0,8"。
    /// 支持 1~4 个值，按 CSS margin 简写规则展开。
    /// </summary>
    public static SlideMlThickness? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        var values = new double[4];
        for (var i = 0; i < parts.Length && i < 4; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                return null;
            }

            values[i] = v;
        }

        return parts.Length switch
        {
            1 => new SlideMlThickness { Left = values[0], Top = values[0], Right = values[0], Bottom = values[0] },
            2 => new SlideMlThickness { Left = values[1], Top = values[0], Right = values[1], Bottom = values[0] },
            3 => new SlideMlThickness { Left = values[1], Top = values[0], Right = values[1], Bottom = values[2] },
            _ => new SlideMlThickness { Left = values[0], Top = values[1], Right = values[2], Bottom = values[3] },
        };
    }
}
