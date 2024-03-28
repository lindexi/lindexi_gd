using System.IO;
using System.Windows.Media.Imaging;
using Uno.UI.Runtime.Skia.Wpf;

using WpfApp = System.Windows.Application;

namespace NujawkeqefunuyeBogearfagallnuhea.WPF;
public partial class App : WpfApp
{
    public App()
    {
        var bitmapImage = new BitmapImage();
        var file = Path.GetFullPath("Image.jpg");
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(file);
        bitmapImage.DecodePixelWidth = 10;
        bitmapImage.DecodePixelHeight = 10;
        bitmapImage.EndInit();
        var width = bitmapImage.PixelWidth;
        var height = bitmapImage.PixelHeight;

        var host = new WpfHost(Dispatcher, () => new AppHead());
        host.Run();
    }
}
