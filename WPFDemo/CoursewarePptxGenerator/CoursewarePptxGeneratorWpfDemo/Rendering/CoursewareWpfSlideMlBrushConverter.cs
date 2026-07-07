using System.Diagnostics;
using System.Windows.Media;
using PptxGenerator.Models.SlideDocuments;

namespace CoursewarePptxGeneratorWpfDemo.Rendering;

/// <summary>
/// 将 SlideML 画刷转换为 WPF 画刷。
/// </summary>
internal static class CoursewareWpfSlideMlBrushConverter
{
    /// <summary>
    /// 将 SlideML 画刷转换为 WPF 画刷。
    /// </summary>
    /// <param name="brush">SlideML 画刷。</param>
    /// <returns>WPF 画刷；无法转换时返回 null。</returns>
    public static Brush? CreateWpfBrush(ISlideMlBrush? brush)
    {
        return brush switch
        {
            SlideMlSolidColorBrush solid => ToSolidColorBrush(solid.Color),
            SlideMlLinearGradientBrush gradient => ToWpfLinearGradientBrush(gradient),
            _ => null,
        };
    }

    private static LinearGradientBrush? ToWpfLinearGradientBrush(SlideMlLinearGradientBrush gradient)
    {
        if (gradient.Stops.Count == 0)
        {
            return null;
        }

        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(gradient.X1, gradient.Y1),
            EndPoint = new System.Windows.Point(gradient.X2, gradient.Y2),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
        };

        foreach (var stop in gradient.Stops)
        {
            brush.GradientStops.Add(new GradientStop(ToWpfColor(stop.Color), stop.Offset));
        }

        return brush;
    }

    private static Color ToWpfColor(string hexColorText)
    {
        var (success, a, r, g, b) = ConvertToColor(hexColorText);
        return success ? Color.FromArgb(a, r, g, b) : Colors.Transparent;
    }

    private static SolidColorBrush? ToSolidColorBrush(string hexColorText)
    {
        var (success, a, r, g, b) = ConvertToColor(hexColorText);
        return success ? new SolidColorBrush(Color.FromArgb(a, r, g, b)) : null;
    }

    internal static (bool success, byte a, byte r, byte g, byte b) ConvertToColor(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return default;
        }

        var startsWithPoundSign = input.StartsWith('#');
        var colorStringLength = input.Length;
        if (startsWithPoundSign)
        {
            colorStringLength -= 1;
        }

        var currentOffset = startsWithPoundSign ? 1 : 0;
        if (colorStringLength is not (8 or 6 or 4 or 3))
        {
            return default;
        }

        var readCount = colorStringLength > 5 ? 2 : 1;
        var includeAlphaChannel = colorStringLength is 8 or 4;
        byte a;

        if (includeAlphaChannel)
        {
            var (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success)
            {
                return default;
            }

            a = result;
            currentOffset += readCount;
        }
        else
        {
            a = 0xFF;
        }

        var (redSuccess, r) = HexCharToNumber(input, currentOffset, readCount);
        if (!redSuccess)
        {
            return default;
        }

        currentOffset += readCount;
        var (greenSuccess, g) = HexCharToNumber(input, currentOffset, readCount);
        if (!greenSuccess)
        {
            return default;
        }

        currentOffset += readCount;
        var (blueSuccess, b) = HexCharToNumber(input, currentOffset, readCount);
        return blueSuccess ? (true, a, r, g, b) : default;
    }

    private static (bool success, byte result) HexCharToNumber(string input, int offset, int readCount)
    {
        Debug.Assert(readCount is 1 or 2, "readCount 只能是 1 或 2。");

        if (offset < 0 || offset + readCount > input.Length)
        {
            return default;
        }

        byte result = 0;
        for (var i = 0; i < readCount; i++, offset++)
        {
            var c = input[offset];
            byte n;
            if (c is >= '0' and <= '9')
            {
                n = (byte)(c - '0');
            }
            else if (c is >= 'a' and <= 'f')
            {
                n = (byte)(c - 'a' + 10);
            }
            else if (c is >= 'A' and <= 'F')
            {
                n = (byte)(c - 'A' + 10);
            }
            else
            {
                return default;
            }

            result *= 16;
            result += n;
        }

        if (readCount == 1)
        {
            result = (byte)(result * 16 + result);
        }

        return (true, result);
    }
}
