using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Converters;

/// <summary>
/// Converts a preview image to an image source.
/// </summary>
public sealed class PreviewImageToImageSourceConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static readonly PreviewImageToImageSourceConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }

        if (value is FilePreviewImage filePreviewImage)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePreviewImage.SourcePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        if (value is IPreviewImage previewImage)
        {
            using var memoryStream = new MemoryStream();
            previewImage.Save(memoryStream);
            if (memoryStream.Length == 0)
            {
                return null;
            }

            memoryStream.Position = 0;
            var decoder = BitmapDecoder.Create(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            frame.Freeze();
            return frame;
        }

        return null;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
