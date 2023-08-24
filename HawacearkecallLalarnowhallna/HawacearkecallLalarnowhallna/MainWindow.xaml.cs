using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HawacearkecallLalarnowhallna;
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

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var size = 300;
        var source = new WriteableBitmap(size, size, 96, 96,
             PixelFormats.Bgra32, null);

        Image.Source = source;

        await Task.Delay(TimeSpan.FromSeconds(1));

        try
        {
            var dest = new int[size * size];// 1 int = 4 byte
            var theErrorStride = 4;
            source.WritePixels(new Int32Rect(0, 0, size, size), dest, theErrorStride, 0);
        }
        catch (ArgumentException)
        {
        }

        var renderTargetBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(Image);
    }
}
