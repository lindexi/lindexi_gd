using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace NarjejerechowainoBuwurjofear.Views;

class AvaSkiaInkCanvas : Control
{
    private int _count;

    public override void Render(DrawingContext context)
    {
        _count++;
        var n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var x = Math.Abs(n) * Bounds.Width;
        _count++;
        n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var y = Math.Abs(n) * Bounds.Height;
        context.Custom(new InkCanvasCustomDrawOperation(new Rect(x, y, 10, 10)));
    }

    class InkCanvasCustomDrawOperation : ICustomDrawOperation
    {
        public InkCanvasCustomDrawOperation(Rect bounds)
        {
            Bounds = bounds;
        }

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
            var skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (skiaSharpApiLeaseFeature == null)
            {
                return;
            }

            using var skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
            var canvas = skiaSharpApiLease.SkCanvas;

            using var skPaint = new SKPaint();
            skPaint.Color = SKColors.Red;
            skPaint.Style = SKPaintStyle.Fill;

            var x = (float) Bounds.X;
            var y = (float) Bounds.Y;

            canvas.DrawRect(x, y, 10, 10, skPaint);
        }

        public Rect Bounds { get; }
    }
}