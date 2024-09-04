using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using NarjejerechowainoBuwurjofear.Inking.Contexts;
using NarjejerechowainoBuwurjofear.Inking.Erasing;
using SkiaSharp;

namespace NarjejerechowainoBuwurjofear.Inking;

public class AvaSkiaInkCanvas : Control
{
    public AvaSkiaInkCanvas()
    {
        EraserMode = new AvaSkiaInkCanvasEraserMode(this);
    }

    public AvaSkiaInkCanvasEraserMode EraserMode { get; }

    public void WritingDown(InkingInputArgs args)
    {
        EnsureInputConflicts();
        var dynamicStrokeContext = new DynamicStrokeContext(args);
        _contextDictionary[args.Id] = dynamicStrokeContext;
        dynamicStrokeContext.Stroke.AddPoint(args.Point);

        InvalidateVisual();
    }

    public void WritingMove(InkingInputArgs args)
    {
        EnsureInputConflicts();
        if (_contextDictionary.TryGetValue(args.Id, out var context))
        {
            context.Stroke.AddPoint(args.Point);
            InvalidateVisual();
        }
    }

    public void WritingUp(InkingInputArgs args)
    {
        EnsureInputConflicts();
        if (_contextDictionary.Remove(args.Id, out var context))
        {
            context.Stroke.AddPoint(args.Point);
            //_staticStrokeDictionary[context.Stroke.Id] = context.Stroke;
            _staticStrokeList.Add(context.Stroke);
            context.Stroke.SetAsStatic();
        }
        InvalidateVisual();
    }

    private readonly Dictionary<int, DynamicStrokeContext> _contextDictionary = [];

    private int _count;
    private List<Rect> _list = [];

    public bool IsWriting => _contextDictionary.Count > 0;

    internal void EnsureInputConflicts()
    {
        if (IsWriting && EraserMode.IsErasing)
        {
            throw new InvalidOperationException("Writing and erasing cannot be performed at the same time.");
        }
    }

    public IReadOnlyList<SkiaStroke> StaticStrokeList => _staticStrokeList;

    private readonly List<SkiaStroke> _staticStrokeList = [];

    //private readonly Dictionary<InkId, SkiaStroke> _staticStrokeDictionary = [];

    //public SkiaStroke GetStaticStroke(InkId id) => _staticStrokeDictionary[id];

    internal void ResetStaticStrokeListEraserResult(IEnumerable<SkiaStroke> skiaStrokeList)
    {
        _staticStrokeList.Clear();
        _staticStrokeList.AddRange(skiaStrokeList);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        _count++;
        var n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var x = Math.Abs(n) * Bounds.Width;
        _count++;
        n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var y = Math.Abs(n) * Bounds.Height;

        _list.Add(new Rect(x, y, 10, 10));

        if (EraserMode.IsErasing)
        {
            EraserMode.Render(context);
        }
        else
        {
            context.Custom(new InkCanvasCustomDrawOperation(this));
        }
    }

    class InkCanvasCustomDrawOperation : ICustomDrawOperation
    {
        public InkCanvasCustomDrawOperation(AvaSkiaInkCanvas inkCanvas)
        {
            var contextDictionary = inkCanvas._contextDictionary;
            _list = [];
            _pathList = [];

            foreach (var strokeContext in contextDictionary.Values)
            {
                var stroke = strokeContext.Stroke;

                var skiaStrokeDrawContext = stroke.CreateDrawContext();
                _pathList.Add(skiaStrokeDrawContext);
            }

            foreach (var skiaStroke in inkCanvas._staticStrokeList)
            {
                var skiaStrokeDrawContext = skiaStroke.CreateDrawContext();
                _pathList.Add(skiaStrokeDrawContext);
            }

            foreach (var skiaStrokeDrawContext in _pathList)
            {
                _list.Add(skiaStrokeDrawContext.DrawBounds);
            }

            if (_list.Count == 0)
            {
                _list = inkCanvas._list;
            }
            var list = _list;

            Rect bounds = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                bounds = bounds.Union(list[i]);
            }
            Bounds = bounds;
        }

        private List<Rect> _list;
        private List<SkiaStrokeDrawContext> _pathList;

        public void Dispose()
        {
            foreach (var skiaStrokeDrawContext in _pathList)
            {
                skiaStrokeDrawContext.Dispose();
            }
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

            if (_pathList.Count > 0)
            {
                skPaint.Color = SKColors.Red;
                skPaint.Style = SKPaintStyle.Fill;

                skPaint.IsAntialias = true;

                skPaint.StrokeWidth = 10;

                foreach (var skiaStrokeDrawContext in _pathList)
                {
                    skPaint.Color = skiaStrokeDrawContext.Color;
                    canvas.DrawPath(skiaStrokeDrawContext.Path, skPaint);
                }

                return;
            }

            skPaint.Color = SKColors.Red;
            skPaint.Style = SKPaintStyle.Fill;

            for (var i = 0; i < _list.Count; i++)
            {
                var bounds = _list[i];
                var x = (float) bounds.X;
                var y = (float) bounds.Y;

                skPaint.Color = new SKColor((uint) (Math.Sin(Math.Pow(Math.E * i, Math.PI)) * int.MaxValue));

                canvas.DrawRect(x, y, 10, 10, skPaint);
            }
        }

        public Rect Bounds { get; }
    }
}