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

namespace WhawlalnelheWigaiyuhaw;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void RepeatButton_OnClick(object sender, RoutedEventArgs e)
    {
        SkiaCanvas.Draw();
    }
}

public class SkiaCanvas : FrameworkElement
{
    public SkiaCanvas()
    {
        Loaded += SkiaCanvas_Loaded;
    }

    public void Draw()
    {
        DrawInner();
    }

    private void SkiaCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        DrawInner();
    }

    private float _ax;
    private float _ay;

    private void DrawInner()
    {
        _ax++;
        _ay++;

        if (_writeableBitmap is null)
        {
            var writeableBitmap = new WriteableBitmap((int) ActualWidth, (int) ActualHeight, 96, 96, PixelFormats.Bgra32,
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
            paint.Style = SKPaintStyle.Fill;
            paint.IsAntialias = true;

            using var typeface = SKFontManager.Default.MatchFamily("微软雅黑");
            using var skFont = new SKFont(typeface, 30);

            paint.Typeface = typeface;
            paint.TextSize = 30;

            var textHeight = 30;

            var text = "ab";

            var baseline = skFont.Metrics.Ascent;

            var skRotationScaleMatrixList = new SKRotationScaleMatrix[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                var skRotationScaleMatrix = SKRotationScaleMatrix.Create(1, float.Pi / 2, 0, textHeight * i, textHeight / 2f, 0f);

                skRotationScaleMatrixList[i] = skRotationScaleMatrix;
            }
            var skTextBlob = SKTextBlob.CreateRotationScale(text, skFont, skRotationScaleMatrixList.AsSpan());

            skCanvas.DrawText(skTextBlob, 10, 50, paint);
            paint.Color = SKColors.Red;
            skCanvas.DrawText(text, 10, 50, paint);
        }

        _writeableBitmap.Lock();
        // 由于 Skia 不支持范围读取，因此这里需要全部刷新
        var dirtyRect = new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight);
        var pixels = skBitmap.GetPixels(out var length);
        var stride = 4 /*RGBA共4个*/ * _writeableBitmap.PixelWidth;
        _writeableBitmap.WritePixels(dirtyRect, pixels,
            (int) _writeableBitmap.PixelWidth * _writeableBitmap.PixelHeight * 4 /*RGBA共4个*/, stride);
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