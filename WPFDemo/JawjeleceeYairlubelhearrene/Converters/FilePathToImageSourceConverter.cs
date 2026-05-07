using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace JawjeleceeYairlubelhearrene.Converters;

internal sealed class FilePathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var filePath = value as string;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return DependencyProperty.UnsetValue;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}