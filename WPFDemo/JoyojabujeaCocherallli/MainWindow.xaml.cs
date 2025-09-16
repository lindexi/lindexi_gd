using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace JoyojabujeaCocherallli;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var file = @"C:\lindexi\Image.png";
        Enhancement(file);
    }

    private void OpenImageFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog()
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;",
            Multiselect = false,
        };
        openFileDialog.ShowDialog(this);
        var file = openFileDialog.FileName;
        Enhancement(file);
    }

    private void Enhancement(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return;
        }

        filePath = Path.GetFullPath(filePath);

        var source = new BitmapImage(new Uri(filePath));
        SourceImage.Source = source;

        var formatConvertedBitmap = new FormatConvertedBitmap();
        formatConvertedBitmap.BeginInit();
        formatConvertedBitmap.Source = source;
        formatConvertedBitmap.DestinationFormat = PixelFormats.Bgra32;
        formatConvertedBitmap.EndInit();

        var sourceBitmap = new WriteableBitmap(formatConvertedBitmap);
        var targetBitmap = new WriteableBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight, sourceBitmap.DpiX,
            sourceBitmap.DpiY, sourceBitmap.Format, sourceBitmap.Palette);

        TuningAdaptiveGammaCorrectionAlgorithm.Enhancement(sourceBitmap, targetBitmap);

        var pngBitmapEncoder = new PngBitmapEncoder();
        pngBitmapEncoder.Frames.Add(BitmapFrame.Create(targetBitmap));
        using var stream = File.Create("1.png");
        pngBitmapEncoder.Save(stream);

        DestImage.Source = targetBitmap;
    }
}

/// <summary>
/// Tuning adaptive gamma correction (TAGC) 低光增强效果算法
/// </summary>
public class TuningAdaptiveGammaCorrectionAlgorithm
{
    public static unsafe void Enhancement(WriteableBitmap sourceBitmap, WriteableBitmap targetBitmap)
    {
        if (sourceBitmap.Format != targetBitmap.Format)
        {
            return;
        }

        if (sourceBitmap.Format != PixelFormats.Bgra32)
        {
            return;
        }

        float inv255 = 1.0f / 255;

        sourceBitmap.Lock();
        targetBitmap.Lock();

        byte* src = (byte*)sourceBitmap.BackBuffer;
        byte* dest = (byte*)targetBitmap.BackBuffer;

        var height = sourceBitmap.PixelHeight;
        var width = sourceBitmap.PixelWidth;
        var stride = sourceBitmap.BackBufferStride;
        int channel = stride / width;

        for (int yIndex = 0; yIndex < height; yIndex++)
        {
            byte* linePixelSource = src + yIndex * stride;
            byte* linePixelDest = dest + yIndex * stride;
            for (int xIndex = 0; xIndex < width; xIndex++)
            {
                float blue = linePixelSource[0] * inv255;
                float green = linePixelSource[1] * inv255;
                float red = linePixelSource[2] * inv255;
                double l = 0.2126f * red + 0.7152 * green + 0.0722 * blue;
                float a = (blue + green + red) / 3;
                double gamma = 5.0f + (0.5f - l) * (1 - a) - 2 * l;
                double y = 2 / gamma;

                byte tb = ClampToByte((int)(Math.Pow(blue, y) * 255 + 0.4999999f));
                double y1 = 2 / gamma;
                byte tg = ClampToByte((int)(Math.Pow(green, y1) * 255 + 0.4999999f));
                double y2 = 2 / gamma;
                byte tr = ClampToByte((int)(Math.Pow(red, y2) * 255 + 0.4999999f));

                byte ta = 0xFF;

                linePixelDest[0] = tb;
                linePixelDest[1] = tg;
                linePixelDest[2] = tr;
                linePixelDest[3] = ta;

                linePixelSource += channel;
                linePixelDest += channel;
            }
        }

        targetBitmap.AddDirtyRect(new Int32Rect(0, 0, targetBitmap.PixelWidth, targetBitmap.PixelHeight));
        sourceBitmap.Unlock();
        targetBitmap.Unlock();
    }

    private static byte ClampToByte(int value)
    {
        return (byte)Math.Clamp(value, 0, byte.MaxValue);
    }
}