using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChatRoom.AvaloniaShell.Converters;

/// <summary>
/// 将角色名哈希为一个稳定的颜色（用于头像背景色）。
/// </summary>
public sealed class RoleColorConverter : IValueConverter
{
    private static readonly SolidColorBrush[] s_palette =
    [
        new(Color.FromRgb(0x42, 0xA5, 0xF5)),
        new(Color.FromRgb(0x66, 0xBB, 0x6A)),
        new(Color.FromRgb(0xFF, 0xA7, 0x26)),
        new(Color.FromRgb(0xEF, 0x5A, 0x5A)),
        new(Color.FromRgb(0xAB, 0x47, 0xBC)),
        new(Color.FromRgb(0x26, 0xC6, 0xDA)),
        new(Color.FromRgb(0xFF, 0xCA, 0x28)),
        new(Color.FromRgb(0x8D, 0x6E, 0x63)),
    ];

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrEmpty(name))
        {
            return s_palette[0];
        }

        int hash = 0;
        foreach (char c in name)
        {
            hash = (hash * 31 + c) & 0x7FFFFFFF;
        }

        return s_palette[hash % s_palette.Length];
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
