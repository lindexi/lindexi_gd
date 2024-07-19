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
using ReewheaberekaiNayweelehe;
using SkiaSharp;

namespace KurwalljurchawrallKakeawelba;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SkiaCanvas.RequireDraw(context =>
        {
            context.SKCanvas.Clear(SKColors.White);

            using var skPaint = new SKPaint();
            skPaint.Color = SKColors.Black;
            skPaint.StrokeWidth = 2;
            skPaint.Style = SKPaintStyle.Stroke;

            for (int y = 0; y < context.SKBitmap.Height; y += 25)
            {
                skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
                context.SKCanvas.DrawLine(0, y, context.SKBitmap.Width, y, skPaint);
            }

            for (int x = 0; x < context.SKBitmap.Width; x += 25)
            {
                skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
                context.SKCanvas.DrawLine(x, 0, x, context.SKBitmap.Height, skPaint);
            }
        });
    }
}

public class SkiaCanvas : FrameworkElement
{
    public SkiaCanvas()
    {
        Loaded += SkiaCanvas_Loaded;
    }

    public event EventHandler<SkiaCanvasContext>? Draw;

    public void RequireDraw()
    {
        if (_isRequireDraw)
        {
            return;
        }

        _isRequireDraw = true;

        Dispatcher.InvokeAsync(DrawInner, DispatcherPriority.Render);
    }

    public void RequireDraw(Action<SkiaCanvasContext> draw)
    {
        Draw += InvokeDraw;
        RequireDraw();

        void InvokeDraw(object? sender, SkiaCanvasContext context)
        {
            Draw -= InvokeDraw;
            draw(context);
        }
    }

    private bool _isRequireDraw = false;

    private void SkiaCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        RequireDraw();
    }

    private void DrawInner()
    {
        if (_writeableBitmap is null)
        {
            var writeableBitmap = new WriteableBitmap((int) ActualWidth, (int) ActualHeight, 96, 96, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent);
            _writeableBitmap = writeableBitmap;
        }

        if (_context is null)
        {
            var skImageInfo = new SKImageInfo()
            {
                Width = _writeableBitmap.PixelWidth,
                Height = _writeableBitmap.PixelHeight,
                ColorType = SKColorType.Bgra8888,
                AlphaType = SKAlphaType.Premul,
                ColorSpace = SKColorSpace.CreateSrgb()
            };

            SkBitmap = new SKBitmap(skImageInfo);
            SkCanvas = new SKCanvas(SkBitmap);

            _context = new SkiaCanvasContext(SkCanvas, SkBitmap);
        }

        Draw?.Invoke(this, _context);

        _writeableBitmap.Lock();
        // 由于 Skia 不支持范围读取，因此这里需要全部刷新
        var dirtyRect = new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight);
        var pixels = _context.SKBitmap.GetPixels(out var length);
        var stride = 4/*RGBA共4个*/ * _writeableBitmap.PixelWidth;
        _writeableBitmap.WritePixels(dirtyRect, pixels, (int) _writeableBitmap.PixelWidth * _writeableBitmap.PixelHeight * 4/*RGBA共4个*/, stride);
        _writeableBitmap.AddDirtyRect(dirtyRect);
        _writeableBitmap.Unlock();

        InvalidateVisual();
    }

    public SKBitmap? SkBitmap { get; private set; }
    public SKCanvas? SkCanvas { get; private set; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_writeableBitmap is not null)
        {
            drawingContext.DrawImage(_writeableBitmap, new Rect(0, 0, ActualWidth, ActualHeight));
        }
    }

    private SkiaCanvasContext? _context;
    private WriteableBitmap? _writeableBitmap;

    #region 输入层

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
    }

    private SkInkCanvas? _inkCanvas;

    #endregion
}

public record SkiaCanvasContext(SKCanvas SKCanvas, SKBitmap SKBitmap);
