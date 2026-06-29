using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator;

/// <summary>
/// 将 SlideML 画刷转换为 WPF 画刷。支持纯色和渐变画刷。
/// </summary>
internal static class WpfSlideMlBrushConverter
{
    /// <summary>
    /// 将 SlideML 画刷转换为 WPF 画刷。支持纯色和渐变画刷。
    /// </summary>
    public static Brush? CreateWpfBrush(ISlideMlBrush? brush)
    {
        return brush switch
        {
            SlideMlSolidColorBrush solid => ToSolidColorBrush(solid.Color),
            SlideMlLinearGradientBrush gradient => ToWpfLinearGradientBrush(gradient),
            _ => null,
        };
    }

    /// <summary>
    /// 将 <see cref="SlideMlLinearGradientBrush"/> 转换为 WPF <see cref="LinearGradientBrush"/>。
    /// </summary>
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
            brush.GradientStops.Add(new GradientStop(
                ToWpfColor(stop.Color),
                stop.Offset));
        }

        return brush;
    }

    /// <summary>
    /// 将十六进制颜色字符串转换为 WPF <see cref="Color"/>。
    /// </summary>
    private static Color ToWpfColor(string hexColorText)
    {
        var (success, a, r, g, b) = ConvertToColor(hexColorText);
        return success ? Color.FromArgb(a, r, g, b) : Colors.Transparent;
    }

    public static SolidColorBrush? ToSolidColorBrush(string hexColorText)
    {
        var (success, a, r, g, b) = ConvertToColor(hexColorText);

        if (!success)
        {
            return null;
        }

        return new SolidColorBrush(Color.FromArgb(a, r, g, b));
    }

    internal static (bool success, byte a, byte r, byte g, byte b) ConvertToColor(string input)
    {
        bool startWithPoundSign = input.StartsWith('#');
        var colorStringLength = input.Length;
        if (startWithPoundSign) colorStringLength -= 1;
        int currentOffset = startWithPoundSign ? 1 : 0;
        // 可以采用的格式如下
        // #FFDFD991   8 个字符 存在 Alpha 通道
        // #DFD991     6 个字符
        // #FD92       4 个字符 存在 Alpha 通道
        // #DAC        3 个字符
        if (colorStringLength == 8
            || colorStringLength == 6
            || colorStringLength == 4
            || colorStringLength == 3)
        {
            bool success;
            byte result;
            byte a;

            int readCount;
            // #DFD991     6 个字符
            // #FFDFD991   8 个字符 存在 Alpha 通道
            //if (colorStringLength == 8 || colorStringLength == 6)
            if (colorStringLength > 5)
            {
                readCount = 2;
            }
            else
            {
                readCount = 1;
            }

            bool includeAlphaChannel = colorStringLength == 8 || colorStringLength == 4;

            if (includeAlphaChannel)
            {
                (success, result) = HexCharToNumber(input, currentOffset, readCount);
                if (!success) return default;
                a = result;
                currentOffset += readCount;
            }
            else
            {
                a = 0xFF;
            }

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte r = result;
            currentOffset += readCount;

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte g = result;
            currentOffset += readCount;

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte b = result;

            return (true, a, r, g, b);
        }

        return default;
    }

    static (bool success, byte result) HexCharToNumber(string input, int offset, int readCount)
    {
        Debug.Assert(readCount == 1 || readCount == 2, "要求 readCount 只能是 1 或者 2 的值，这是框架限制，因此不做判断");

        byte result = 0;

        for (int i = 0; i < readCount; i++, offset++)
        {
            var c = input[offset];
            byte n;
            if (c >= '0' && c <= '9')
            {
                n = (byte) (c - '0');
            }
            else if (c >= 'a' && c <= 'f')
            {
                n = (byte) (c - 'a' + 10);
            }
            else if (c >= 'A' && c <= 'F')
            {
                n = (byte) (c - 'A' + 10);
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
            result = (byte) (result * 16 + result);
        }

        return (true, result);
    }
}
