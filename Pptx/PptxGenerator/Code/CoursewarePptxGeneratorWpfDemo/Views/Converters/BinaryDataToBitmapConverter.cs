using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CoursewarePptxGeneratorWpfDemo.Views.Converters;

/// <summary>
/// Converts binary image data to a bitmap image.
/// </summary>
public sealed class BinaryDataToBitmapConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static readonly BinaryDataToBitmapConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not BinaryData data)
        {
            return null;
        }

        using var stream = data.ToStream();
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
