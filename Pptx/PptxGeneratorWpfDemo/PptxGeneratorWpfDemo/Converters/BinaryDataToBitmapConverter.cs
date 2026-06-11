using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PptxGeneratorWpfDemo.Converters;

/// <summary>
/// 将 <see cref="BinaryData"/> 转换为 WPF <see cref="BitmapImage"/>，用于 <see cref="AgentLib.Model.CopilotChatImageItem"/> 的图片渲染。
/// </summary>
public sealed class BinaryDataToBitmapConverter : IValueConverter
{
    /// <summary>
    /// 单例实例。
    /// </summary>
    public static readonly BinaryDataToBitmapConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BinaryData binaryData || binaryData.ToMemory().IsEmpty)
        {
            return null;
        }

        try
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = new MemoryStream(binaryData.ToArray());
            bitmapImage.EndInit();
            return bitmapImage;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
