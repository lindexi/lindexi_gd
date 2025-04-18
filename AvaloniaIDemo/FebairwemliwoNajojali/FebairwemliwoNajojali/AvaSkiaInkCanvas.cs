using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using InkBase;

using SkiaSharp;

namespace FebairwemliwoNajojali;

public class AvaSkiaInkCanvas : Control
{
    public AvaSkiaInkCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    private IWpfInkLayer InkingAcceleratorLayer => WpfForAvaloniaInkingAccelerator.Instance.InkLayer;

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
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
        SKCanvas canvas = skiaSharpApiLease.SkCanvas;

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