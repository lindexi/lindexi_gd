using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChatRoom.AvaloniaShell.Converters;

/// <summary>
/// 布尔值转可见性转换器。
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        return boolValue;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b;
    }
}

/// <summary>
/// 根据模型信息和控件可用宽度判断是否显示模型信息。
/// </summary>
public sealed class ModelDisplayVisibilityConverter : IMultiValueConverter
{
    private const double DefaultMinimumWidth = 520;

    /// <inheritdoc/>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not bool hasModelDisplayName || !hasModelDisplayName)
        {
            return false;
        }

        if (values[1] is not double width || double.IsNaN(width) || double.IsInfinity(width))
        {
            return false;
        }

        double minimumWidth = DefaultMinimumWidth;
        if (parameter is string parameterText && double.TryParse(parameterText, NumberStyles.Float, culture, out double parsedMinimumWidth))
        {
            minimumWidth = parsedMinimumWidth;
        }

        return width >= minimumWidth;
    }
}

/// <summary>
/// 布尔取反转换器。
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}
