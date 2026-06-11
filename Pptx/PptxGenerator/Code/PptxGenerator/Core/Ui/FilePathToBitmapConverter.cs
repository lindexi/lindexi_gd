using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// 将文件路径字符串转换为 <see cref="Bitmap"/>，用于 Image.Source 绑定。
/// </summary>
public sealed class FilePathToBitmapConverter : IValueConverter
{
    /// <summary>
    /// 单例实例，供 XAML 编译绑定使用。
    /// </summary>
    public static readonly FilePathToBitmapConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrWhiteSpace(filePath) &&
            System.IO.File.Exists(filePath))
        {
            return new Bitmap(filePath);
        }

        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
