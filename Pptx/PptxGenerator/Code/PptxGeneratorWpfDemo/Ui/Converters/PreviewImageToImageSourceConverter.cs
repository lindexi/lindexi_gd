using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using PptxGenerator;
using PptxGenerator.Models;

namespace PptxGeneratorWpfDemo.Converters;

/// <summary>
/// 将 <see cref="IPreviewImage"/> 转换为 WPF <see cref="ImageSource"/>，用于 Image.Source 绑定。
/// 支持 <see cref="WpfPreviewImage"/>（直接取 Source）、<see cref="FilePreviewImage"/>（从路径加载）
/// 以及任意 <see cref="IPreviewImage"/> 实现（Save 到 MemoryStream 后再加载）。
/// </summary>
public sealed class PreviewImageToImageSourceConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }

        if (value is WpfPreviewImage wpfPreviewImage)
        {
            return wpfPreviewImage.Source;
        }

        if (value is FilePreviewImage filePreviewImage)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePreviewImage.SourcePath);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        // 通用 fallback：通过 Save(Stream) 加载
        if (value is IPreviewImage previewImage)
        {
            using var memoryStream = new MemoryStream();
            previewImage.Save(memoryStream);
            memoryStream.Position = 0;
            var decoder = BitmapDecoder.Create(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            frame.Freeze();
            return frame;
        }

        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
