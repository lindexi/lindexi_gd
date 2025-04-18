using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace FebairwemliwoNajojali;

public class AvaSkiaInkCanvas : Control
{
    public AvaSkiaInkCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public override void Render(DrawingContext context)
    {
        var bounds = this.Bounds;
        context.Custom(new InkCanvasCustomDrawOperation()
        {
            Bounds = bounds
        });
    }
}

file class InkCanvasCustomDrawOperation : ICustomDrawOperation
{
    public Rect Bounds { get; set; }

    public bool HitTest(Point p)
    {
        return true;
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

        //canvas.Clear(new SKColor(0x5c, 0x56, 0x56, 0xff));
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(other, this);
    }

    public void Dispose()
    {
        
    }
}