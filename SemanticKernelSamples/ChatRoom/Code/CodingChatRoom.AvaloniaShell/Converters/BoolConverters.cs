using System;
using System.Globalization;

using Avalonia.Data.Converters;

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
