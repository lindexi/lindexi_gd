using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace JawjeleceeYairlubelhearrene.Converters;

internal sealed class FilePathToUriConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var filePath = value as string;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        return new Uri(filePath, UriKind.Absolute);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}