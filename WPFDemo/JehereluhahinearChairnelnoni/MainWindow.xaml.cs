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

namespace JehereluhahinearChairnelnoni;

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
        var renderTargetBitmap = new RenderTargetBitmap((int) Math.Floor(RootGrid.ActualWidth), (int) Math.Floor(RootGrid.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(RootGrid);

        var jpegBitmapEncoder = new JpegBitmapEncoder();
        jpegBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
        using var fileStream = System.IO.File.Create("JehereluhahinearChairnelnoni.jpg");
        jpegBitmapEncoder.Save(fileStream);
    }
}