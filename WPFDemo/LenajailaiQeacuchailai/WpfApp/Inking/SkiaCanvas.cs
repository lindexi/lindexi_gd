using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace WpfApp.Inking;

public class SkiaCanvas : FrameworkElement
{
    public SkiaCanvas()
    {
        Loaded += SkiaCanvas_Loaded;
    }

    public void Draw(Action<SKCanvas> draw)
    {
        _draw = draw;
        DrawInner();
    }

    private Action<SKCanvas>? _draw;

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
            _draw?.Invoke(skCanvas);
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