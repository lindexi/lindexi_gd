using System.Globalization;

namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 四角独立圆角值。
/// </summary>
public readonly record struct SlideMlCornerRadius
{
    /// <summary>
    /// 左上角圆角半径。
    /// </summary>
    public double TopLeft { get; init; }

    /// <summary>
    /// 右上角圆角半径。
    /// </summary>
    public double TopRight { get; init; }

    /// <summary>
    /// 右下角圆角半径。
    /// </summary>
    public double BottomRight { get; init; }

    /// <summary>
    /// 左下角圆角半径。
    /// </summary>
    public double BottomLeft { get; init; }

    /// <summary>
    /// 从单个值隐式转换（四角统一）。
    /// </summary>
    public static implicit operator SlideMlCornerRadius(double uniformRadius)
        => new()
        {
            TopLeft = uniformRadius,
            TopRight = uniformRadius,
            BottomRight = uniformRadius,
            BottomLeft = uniformRadius,
        };

    /// <summary>
    /// 从逗号分隔的字符串解析，如 "8,16,8,16"。
    /// 支持 1~4 个值，按 CSS border-radius 简写规则展开。
    /// </summary>
    public static SlideMlCornerRadius? Parse(string? text)
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
            1 => new SlideMlCornerRadius { TopLeft = values[0], TopRight = values[0], BottomRight = values[0], BottomLeft = values[0] },
            2 => new SlideMlCornerRadius { TopLeft = values[0], TopRight = values[1], BottomRight = values[0], BottomLeft = values[1] },
            3 => new SlideMlCornerRadius { TopLeft = values[0], TopRight = values[1], BottomRight = values[2], BottomLeft = values[1] },
            _ => new SlideMlCornerRadius { TopLeft = values[0], TopRight = values[1], BottomRight = values[2], BottomLeft = values[3] },
        };
    }
}
