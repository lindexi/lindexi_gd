using System;
using System.Globalization;
using System.IO;

using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// 将 <see cref="BinaryData"/> 转换为 Avalonia <see cref="Bitmap"/>，用于 <see cref="AgentLib.Model.CopilotChatImageItem"/> 的图片渲染。
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
            return new Bitmap(new MemoryStream(binaryData.ToArray()));
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
