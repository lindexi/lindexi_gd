using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NenerehalbarKerecairearkem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var width = 3840;
        var height = 2160;

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

        var stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);

        var pixels = new byte[height * stride];

        var file = @"c:\lindexi\Work\2";
        var byteList = File.ReadAllBytes(file);

        for (int col = 0; col < width; col++)
        {
            for (int row = 0; row < height; row++)
            {
                var byteIndex = col * width * row * 4;

                if (byteIndex >= byteList.Length)
                {
                    break;
                }

                var b = byteList[byteIndex];
                var g = byteList[byteIndex + 1];
                var r = byteList[byteIndex + 2];
                var a = byteList[byteIndex + 3];

                pixels[byteIndex] = b;
                pixels[byteIndex + 1] = g;
                pixels[byteIndex + 2] = r;
                pixels[byteIndex + 3] = 0xff;
            }
        }

        bitmap.Lock();
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        bitmap.Unlock();

        Image.Source = bitmap;
        Image.Stretch = Stretch.Fill;
        Image.Width = 1000;
        Image.Height = 1000;
    }
}