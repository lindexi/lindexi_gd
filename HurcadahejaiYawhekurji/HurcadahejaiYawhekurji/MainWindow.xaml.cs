using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using Path = System.IO.Path;

namespace HurcadahejaiYawhekurji;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(new Size(100, 10)));
            drawingContext.DrawLine(new Pen(Brushes.Black, 2), new Point(2, 5), new Point(90, 5));
        }

        var dpiScale = VisualTreeHelper.GetDpi(this);
        var renderTargetBitmap = new RenderTargetBitmap(100, 10, dpiScale.PixelsPerInchX, dpiScale.PixelsPerInchY,
            PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);

        var jpegBitmapEncoder = new JpegBitmapEncoder();
        var bitmapMetadata = new BitmapMetadata("jpg")
        {
            Title = "旋转的图片",
            Author = new ReadOnlyCollection<string>(new[] { "林德熙" }),
            Comment = "这是备注",
            Copyright = "版权",
            Subject = "主题",
            ApplicationName = "应用",
        };
        const int Rotate90 = 6;
        bitmapMetadata.SetQuery("System.Photo.Orientation", Rotate90);

        var bitmapFrame = BitmapFrame.Create(renderTargetBitmap, thumbnail: null, bitmapMetadata,
            new ReadOnlyCollection<ColorContext>(new List<ColorContext>()));
        jpegBitmapEncoder.Frames.Add(bitmapFrame);

        var file = Path.GetTempFileName() + ".jpg";
        using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite))
        {
            jpegBitmapEncoder.Save(fileStream);
        }

        Image.Source = new BitmapImage(new Uri(file));

        Decode(file);
    }

    private void Decode(string file)
    {
        var decoder = BitmapDecoder.Create
        (
            new Uri(file, UriKind.Absolute),
            BitmapCreateOptions.DelayCreation,
            BitmapCacheOption.None
        );

        var frame = decoder.Frames[0];

        var rotation = GetRotation(frame);

        var size = rotation is Rotation.Rotate90 or Rotation.Rotate270
            ? new Size(frame.PixelHeight, frame.PixelWidth)
            : new Size(frame.PixelWidth, frame.PixelHeight);
    }

    private Rotation GetRotation(BitmapFrame frame)
    {
        const string query = "System.Photo.Orientation";
        return frame.Metadata is BitmapMetadata bitmapMetadata
               && bitmapMetadata.ContainsQuery(query)
               && bitmapMetadata.GetQuery(query) is ushort orientation
            ? orientation switch
            {
                6 => Rotation.Rotate90,
                3 => Rotation.Rotate180,
                8 => Rotation.Rotate270,
                _ => Rotation.Rotate0,
            }
            : Rotation.Rotate0;
    }
}