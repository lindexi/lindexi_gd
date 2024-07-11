using SkiaSharp;

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

namespace WhaleljainaDeljayfecelchalearqe;

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
        await Task.Delay(100);
        Image.Draw(canvas =>
        {
            canvas.Clear(SKColors.Red);
        });
    }
}

public class SkiaCanvas : Image
{
    public SkiaCanvas()
    {
        Loaded += SkiaCanvas_Loaded;
    }

    private void SkiaCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        var writeableBitmap = new WriteableBitmap(PixelWidth, PixelHeight, 96, 96, PixelFormats.Bgra32,
            BitmapPalettes.Halftone256Transparent);

        _writeableBitmap = writeableBitmap;

        var skImageInfo = new SKImageInfo()
        {
            Width = PixelWidth,
            Height = PixelHeight,
            ColorType = SKColorType.Bgra8888,
            AlphaType = SKAlphaType.Premul,
            ColorSpace = SKColorSpace.CreateSrgb()
        };

        SkBitmap = new SKBitmap(skImageInfo);

        //SKSurface surface = SKSurface.Create(skImageInfo, writeableBitmap.BackBuffer);
        //SkSurface = surface;
        //SkCanvas = surface.Canvas;
        SkCanvas = new SKCanvas(SkBitmap);
        SkCanvas.Clear();
        SkCanvas.Flush();

        Source = writeableBitmap;

        Draw(canvas =>
        {
            //var eraserView = new EraserView();
            //using var skBitmap = eraserView.GetEraserView(30, 45);
            //canvas.DrawBitmap(skBitmap, 100, 100);
            canvas.Clear(SKColors.White);
        });
    }

    public void Draw(Action<SKCanvas> action)
    {
        Draw(canvas =>
        {
            action(canvas);
            return null;
        });
    }

    public void Draw(Func<SKCanvas, Int32Rect?> draw)
    {
        var writeableBitmap = _writeableBitmap;
        writeableBitmap.Lock();

        var canvas = SkCanvas;
        var dirtyRect = draw(canvas);
        canvas.Flush();

        dirtyRect ??= new Int32Rect(0, 0, PixelWidth, PixelHeight);
        var pixels = SkBitmap.GetPixels(out var length);
        var stride = 4 * PixelWidth;
        writeableBitmap.WritePixels(dirtyRect.Value, pixels, (int) PixelWidth * PixelHeight * 4, stride);
        writeableBitmap.AddDirtyRect(dirtyRect.Value);
        writeableBitmap.Unlock();
    }

    private WriteableBitmap _writeableBitmap = null!; // 这里的 null! 是 C# 的新语法，是给智能分析用的，表示这个字段在使用的时候不会为空
                                                      //public SKSurface SkSurface { get; private set; } = null!; // 实际上 null! 的含义是我明确给他一个空值，也就是说如果是空也是预期的
    public SKBitmap SkBitmap { get; private set; } = null!; // 实际上 null! 的含义是我明确给他一个空值，也就是说如果是空也是预期的
    public SKCanvas SkCanvas { get; private set; } = null!; // 实际上 null! 的含义是我明确给他一个空值，也就是说如果是空也是预期的

    public int PixelWidth => (int) Width;
    public int PixelHeight => (int) Height;

    public void Update()
    {
        var writeableBitmap = _writeableBitmap;
        writeableBitmap.Lock();

        //var dirtyRect = new Int32Rect((int)rect.X, (int)rect.Y, (int) rect.Width, (int) rect.Height);
        //dirtyRect = new Int32Rect(100, 100, 600, 600);
        // 由于 Skia 不支持范围读取，因此这里需要全部刷新
        var dirtyRect = new Int32Rect(0, 0, PixelWidth, PixelHeight);

        var pixels = SkBitmap.GetPixels(out var length);
        var stride = 4 * PixelWidth;
        writeableBitmap.WritePixels(dirtyRect, pixels, (int) PixelWidth * PixelHeight * 4, stride);
        writeableBitmap.AddDirtyRect(dirtyRect);
        writeableBitmap.Unlock();
    }
}