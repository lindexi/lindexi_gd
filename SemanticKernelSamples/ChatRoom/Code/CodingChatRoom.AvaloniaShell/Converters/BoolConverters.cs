using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CodingChatRoom.AvaloniaShell.Converters;

/// <summary>
/// 将布尔值取反。
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}

/// <summary>
/// 根据窗口实际获得的透明级别选择 Mica 背景或浅色回退背景。
/// </summary>
public sealed class WindowTransparencyBackgroundConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is WindowTransparencyLevel level && level == WindowTransparencyLevel.Mica
            ? Brushes.Transparent
            : Brushes.White;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}