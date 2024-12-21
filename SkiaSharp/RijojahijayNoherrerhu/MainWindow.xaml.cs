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
using System.Windows.Threading;

namespace RijojahijayNoherrerhu;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

public class SkiaCanvas : FrameworkElement
{
    public SkiaCanvas()
    {
        Loaded += SkiaCanvas_Loaded;
    }

    private void SkiaCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        DrawInner();
    }

    private void DrawInner()
    {
        if (_writeableBitmap is null)
        {
            var writeableBitmap = new WriteableBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent);
            _writeableBitmap = writeableBitmap;
        }

        var skImageInfo = new SKImageInfo()
        {
            Width = _writeableBitmap.PixelWidth,
            Height = _writeableBitmap.PixelHeight,
            ColorType = SKColorType.Bgra8888,
            AlphaType = SKAlphaType.Premul,
            ColorSpace = SKColorSpace.CreateSrgb()
        };

        var skBitmap = new SKBitmap(skImageInfo);

        using (var skCanvas = new SKCanvas(skBitmap))
        {
            using var paint = new SKPaint();
            paint.Color = SKColors.Blue;
            paint.Style = SKPaintStyle.Stroke;
            paint.IsAntialias = true;

            var marginLeft = 35;
            var marginTop = 30;
            skCanvas.Clear(SKColors.White);
            skCanvas.DrawRect(marginLeft, marginTop, 300, 300, paint);

            // 在基线绘制一条线，用于测试字体的基线
            var baseline = 100;
            skCanvas.DrawLine(marginLeft, baseline + marginTop, 300 + marginLeft, baseline + marginTop, paint);
            // 附带加上刻度
            paint.Typeface = SKTypeface.FromFamilyName("微软雅黑");
            paint.TextSize = 15;
            paint.Color = SKColors.Black;
            paint.Style = SKPaintStyle.Fill;
            skCanvas.DrawText(0.ToString(), 25, marginTop, paint);
            skCanvas.DrawText(baseline.ToString(), 5, baseline + marginTop, paint);

            // 尝试绘制文本内容
            var text = "asdfghijpqWER";
            paint.TextSize = 30;
            skCanvas.DrawText(text, marginLeft, baseline + marginTop, paint);
        }

        _writeableBitmap.Lock();
        // 由于 Skia 不支持范围读取，因此这里需要全部刷新
        var dirtyRect = new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight);
        var pixels = skBitmap.GetPixels(out var length);
        var stride = 4 /*RGBA共4个*/ * _writeableBitmap.PixelWidth;
        _writeableBitmap.WritePixels(dirtyRect, pixels,
            (int)_writeableBitmap.PixelWidth * _writeableBitmap.PixelHeight * 4 /*RGBA共4个*/, stride);
        _writeableBitmap.AddDirtyRect(dirtyRect);
        _writeableBitmap.Unlock();

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_writeableBitmap is not null)
        {
            drawingContext.DrawImage(_writeableBitmap, new Rect(0, 0, ActualWidth, ActualHeight));
        }
    }

    private WriteableBitmap? _writeableBitmap;
}