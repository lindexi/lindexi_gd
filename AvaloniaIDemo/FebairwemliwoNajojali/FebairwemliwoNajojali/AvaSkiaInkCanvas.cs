using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;

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
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            SkiaStrokeList.Add(e);
            _toClearList.Add(e);
            InvalidateVisual();
        });
    }

    private readonly List<SkiaStroke> _toClearList = [];

    public List<SkiaStroke> SkiaStrokeList { get; } = [];

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
        var bounds = Bounds;
        var inkCanvasCustomDrawOperation = new InkCanvasCustomDrawOperation()
        {
            Bounds = bounds,
            SkiaStrokeList = SkiaStrokeList.ToList(),
            ToClearList = _toClearList.ToList(),
        };
        _toClearList.Clear();
        context.Custom(inkCanvasCustomDrawOperation);

        if (ElementComposition.GetElementVisual(this) is { } selfVisual)
        {
            Compositor compositor = selfVisual.Compositor;
            CompositionBatch batch = compositor.RequestCompositionBatchCommitAsync();
            batch.Rendered.ContinueWith(_ =>
            {
                foreach (var skiaStroke in inkCanvasCustomDrawOperation.ToClearList)
                {
                    InkingAcceleratorLayer.HideStroke(skiaStroke);
                }
            });
        }
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
    public required List<SkiaStroke> SkiaStrokeList { get; init; }
    public required List<SkiaStroke> ToClearList { get; init; }

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
        using var paint = new SKPaint();
        foreach (var skiaStroke in SkiaStrokeList)
        {
            paint.Style = SKPaintStyle.Fill;
            paint.Color = new SKColor(skiaStroke.Color.R, skiaStroke.Color.G, skiaStroke.Color.B, skiaStroke.Color.A);
            paint.IsAntialias = true;

            canvas.DrawPath(skiaStroke.InkPath, paint);
        }
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