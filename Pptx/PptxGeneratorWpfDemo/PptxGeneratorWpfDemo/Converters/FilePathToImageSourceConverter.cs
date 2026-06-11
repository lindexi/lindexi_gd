using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PptxGeneratorWpfDemo.Converters;

/// <summary>
/// 将文件路径字符串转换为 <see cref="BitmapImage"/>，用于 Image.Source 绑定。
/// </summary>
public sealed class FilePathToImageSourceConverter : IValueConverter
{
    /// <summary>
    /// 单例实例，供 XAML 编译绑定使用。
    /// </summary>
    public static readonly FilePathToImageSourceConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrWhiteSpace(filePath) &&
            System.IO.File.Exists(filePath))
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
