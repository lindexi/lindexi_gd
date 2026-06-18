using System;
using System.Globalization;
using System.Text;

using Avalonia.Data.Converters;

namespace ChatRoom.AvaloniaShell.Converters;

/// <summary>
/// 将角色名转换为首字（用于头像显示）。
/// </summary>
public sealed class RoleInitialConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrEmpty(name))
        {
            return "?";
        }

        return char.ToUpper(name[0], CultureInfo.InvariantCulture).ToString();
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
