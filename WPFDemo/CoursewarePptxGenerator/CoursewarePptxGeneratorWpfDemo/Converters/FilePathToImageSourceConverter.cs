using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CoursewarePptxGeneratorWpfDemo.Converters;

/// <summary>
/// Converts a file path to an image source.
/// </summary>
public sealed class FilePathToImageSourceConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static readonly FilePathToImageSourceConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
