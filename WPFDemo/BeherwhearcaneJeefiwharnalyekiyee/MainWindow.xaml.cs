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

namespace BeherwhearcaneJeefiwharnalyekiyee;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var width = 256;
        var height = 256;

        var bounds = Path.Data.Bounds;
        var scaleX = width / bounds.Width;
        var scaleY = height / bounds.Height;

        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.PushTransform(new ScaleTransform(scaleX, scaleY));
            drawingContext.PushTransform(new TranslateTransform(Math.Min(0, -bounds.X), Math.Min(0, -bounds.Y)));

            drawingContext.DrawGeometry(Path.Fill, null, Path.Data);
        }

        var renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);

        var pngBitmapEncoder = new PngBitmapEncoder();
        pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
        var file = "1.png";
        using var fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite);
        pngBitmapEncoder.Save(fileStream);
    }
}