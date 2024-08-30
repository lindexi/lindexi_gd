using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using SkiaSharp;

namespace BiranairfelKadeayelafeahule.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        RootGrid.PointerMoved += RootGrid_PointerMoved;
    }

    private void RootGrid_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        Foo.Start();
    }
}

class Foo : Control
{
    public void Start()
    {
        lock (_locker)
        {
            if (_context is null)
            {
                var stopwatch = Stopwatch.StartNew();
                _context = new Context(stopwatch);

                InvalidateVisual();
            }
        }
    }

    private Context? _context;
    private readonly object _locker = new object();

    record Context(Stopwatch Stopwatch);

    public override void Render(DrawingContext context)
    {
        lock (_locker)
        {
            if (_context is not null)
            {
                _context.Stopwatch.Stop();
                var elapsed = _context.Stopwatch.ElapsedMilliseconds;
                context.DrawText(new FormattedText($"Render Elapsed={elapsed}", CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight, Typeface.Default, 20, Brushes.Black), new Point(10, 10));

                _context.Stopwatch.Start();
                context.Custom(new CustomDrawOperation(this));
            }
        }
    }

    class CustomDrawOperation : ICustomDrawOperation
    {
        public CustomDrawOperation(Foo foo)
        {
            _foo = foo;
            Bounds = new Rect(0, 0, _foo.Bounds.Width, _foo.Bounds.Height);
        }

        private readonly Foo _foo;

        public void Dispose()
        {
            
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            lock (_foo._locker)
            {
                if (_foo._context is not null)
                {
                    _foo._context.Stopwatch.Stop();
                    var elapsed = _foo._context.Stopwatch.ElapsedMilliseconds;

                    var skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                    if (skiaSharpApiLeaseFeature == null)
                    {
                        return;
                    }

                    using var skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
                    var canvas = skiaSharpApiLease.SkCanvas;
                    using var skPaint = new SKPaint();
                    skPaint.Typeface = SKTypeface.Default;
                    skPaint.Color = SKColors.Black;

                    canvas.DrawText($"CustomDrawOperation Elapsed={elapsed}", 10, 100, skPaint);
                }

                _foo._context = null;
            }
        }

        public Rect Bounds { get; }
    }
}

