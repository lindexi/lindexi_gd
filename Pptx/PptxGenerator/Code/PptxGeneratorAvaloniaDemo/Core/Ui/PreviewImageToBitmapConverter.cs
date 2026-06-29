using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using PptxGenerator.Models;

namespace PptxGenerator;

/// <summary>
/// 将 <see cref="IPreviewImage"/> 转换为 Avalonia <see cref="Bitmap"/>，用于 Image.Source 绑定。
/// 支持 <see cref="AvaloniaPreviewImage"/>（直接取 Source）、<see cref="FilePreviewImage"/>（从路径加载）
/// 以及任意 <see cref="IPreviewImage"/> 实现（Save 到 MemoryStream 后再加载）。
/// </summary>
public sealed class PreviewImageToBitmapConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }

        if (value is AvaloniaPreviewImage avaloniaPreviewImage)
        {
            return avaloniaPreviewImage.Source;
        }

        if (value is FilePreviewImage filePreviewImage)
        {
            return new Bitmap(filePreviewImage.SourcePath);
        }

        // 通用 fallback：通过 Save(Stream) 加载
        if (value is IPreviewImage previewImage)
        {
            using var memoryStream = new MemoryStream();
            previewImage.Save(memoryStream);
            memoryStream.Position = 0;
            return new Bitmap(memoryStream);
        }

        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
