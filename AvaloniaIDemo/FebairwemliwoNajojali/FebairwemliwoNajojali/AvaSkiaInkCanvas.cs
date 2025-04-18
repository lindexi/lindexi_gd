using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using InkBase;

using NarjejerechowainoBuwurjofear.Inking.Contexts;

using SkiaSharp;

namespace FebairwemliwoNajojali;

public class AvaSkiaInkCanvas : Control
{
    public AvaSkiaInkCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        InkingAcceleratorLayer.StrokeCollected += InkingAcceleratorLayer_StrokeCollected;
    }

    private void InkingAcceleratorLayer_StrokeCollected(object? sender, SkiaStroke e)
    {
    }

    private IWpfInkLayer InkingAcceleratorLayer => WpfForAvaloniaInkingAccelerator.Instance.InkLayer;

    private readonly Dictionary<int /*PointerId*/, InkDynamicDrawingContext> _dictionary = [];

    private readonly Dictionary<InkId, InkDynamicDrawingContext> _staticInkDynamicDrawingContextDictionary = [];

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _dictionary.Add(e.Pointer.Id, new InkDynamicDrawingContext());
        var inkPoint = AddPoint(e);

        InkingAcceleratorLayer.Down(inkPoint);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_dictionary.TryGetValue(e.Pointer.Id, out var inkDynamicDrawingContext))
        {
            var inkPoint = AddPoint(e);

            InkingAcceleratorLayer.Move(inkPoint);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dictionary.Remove(e.Pointer.Id, out InkDynamicDrawingContext? inkDynamicDrawingContext))
        {
            var inkPoint = AddPoint(e, inkDynamicDrawingContext);

            InkingAcceleratorLayer.Up(inkPoint);

            _staticInkDynamicDrawingContextDictionary[inkDynamicDrawingContext.InkId] = inkDynamicDrawingContext;
        }
    }

    private InkPoint AddPoint(PointerEventArgs e, InkDynamicDrawingContext? inkDynamicDrawingContext = null)
    {
        inkDynamicDrawingContext ??= _dictionary[e.Pointer.Id];
        var currentPoint = e.GetCurrentPoint(this);
        var (x, y) = currentPoint.Position;
        var inkPoint = new InkPoint(inkDynamicDrawingContext.InkId, x, y);
        inkDynamicDrawingContext.PointList.Add(inkPoint);
        return inkPoint;
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

class InkDynamicDrawingContext
{
    public InkDynamicDrawingContext()
    {
        InkId = InkId.NewId();
    }

    public InkId InkId { get; }

    public List<InkPoint> PointList { get; } = [];
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