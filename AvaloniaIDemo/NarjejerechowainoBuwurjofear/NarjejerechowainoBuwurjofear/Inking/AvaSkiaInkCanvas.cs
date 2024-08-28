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
    private List<Rect> _list = [];

    public override void Render(DrawingContext context)
    {
        _count++;
        var n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var x = Math.Abs(n) * Bounds.Width;
        _count++;
        n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var y = Math.Abs(n) * Bounds.Height;

        _list.Add(new Rect(x, y, 10, 10));

        context.Custom(new InkCanvasCustomDrawOperation(_list));
    }

    class InkCanvasCustomDrawOperation : ICustomDrawOperation
    {
        public InkCanvasCustomDrawOperation(List<Rect> list)
        {
            Rect bounds = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                bounds = bounds.Union(list[i]);
            }
            Bounds = bounds;

            _list = list;
        }

        private List<Rect> _list;

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

            for (var i = 0; i < _list.Count; i++)
            {
                var bounds = _list[i];
                var x = (float)bounds.X;
                var y = (float)bounds.Y;

                skPaint.Color = new SKColor((uint)(Math.Sin(Math.Pow(Math.E * i, Math.PI)) * int.MaxValue));

                canvas.DrawRect(x, y, 10, 10, skPaint);
            }
        }

        public Rect Bounds { get; }
    }
}