using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat;

internal sealed class ToolImagePreviewConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ToolImage image) return null;
        try
        {
            using var stream = new MemoryStream(System.Convert.FromBase64String(image.Base64), writable: false);
            var preview = new BitmapImage();
            preview.BeginInit();
            preview.CacheOption = BitmapCacheOption.OnLoad;
            preview.StreamSource = stream;
            preview.EndInit();
            preview.Freeze();
            return preview;
        }
        catch (Exception exception) when (exception is FormatException or NotSupportedException or ArgumentException or IOException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            ChatDiagnostics.Failure("DecodeToolImage", exception, "MediaType", image.MediaType);
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
