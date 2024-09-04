using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using NarjejerechowainoBuwurjofear.Inking.Erasing;
using NarjejerechowainoBuwurjofear.Inking.Utils;
using SkiaSharp;

using UnoInk.Inking.InkCore;

namespace NarjejerechowainoBuwurjofear.Inking;

public class SkiaStroke : IDisposable
{
    public SkiaStroke(InkId id)
    {
        Path = new SKPath();
        Id = id;
    }

    public InkId Id { get; init; }

    public SKPath Path { get; }

    public SKColor Color { get; set; } = SKColors.Red;
    public float Width { get; set; } = 20;

    public List<StylusPoint> PointList { get; } = [];

    /// <summary>
    /// 是否需要重新创建笔迹点，采用平滑滤波算法
    /// </summary>
    public static bool ShouldReCreatePoint { get; set; } = false;

    public void AddPoint(StylusPoint point)
    {
        if (PointList.Count > 0)
        {
            var lastPoint = PointList[^1];
            if (lastPoint == point)
            {
                // 如果两个点相同，则丢点
                return;
            }
        }

        PointList.Add(point);

        var pointList = PointList;
        if (ShouldReCreatePoint && pointList.Count > 10)
        {
            pointList = ApplyMeanFilter(pointList);
        }

        if (pointList.Count > 2)
        {
            var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, Width);

            Path.Reset();
            Path.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        }
        else
        {
        }
    }

    public void Dispose()
    {
        Path.Dispose();
    }

    public static List<StylusPoint> ApplyMeanFilter(List<StylusPoint> pointList, int step = 10)
    {
        var xList = ApplyMeanFilter(pointList.Select(t => t.Point.X).ToList(), step);
        var yList = ApplyMeanFilter(pointList.Select(t => t.Point.Y).ToList(), step);

        var newPointList = new List<StylusPoint>();
        for (int i = 0; i < xList.Count && i < yList.Count; i++)
        {
            newPointList.Add(new StylusPoint(xList[i], yList[i]));
        }

        return newPointList;
    }

    public static List<double> ApplyMeanFilter(List<double> list, int step)
    {
        // 前面一半加不了
        var newList = new List<double>(list.Take(step / 2));
        for (int i = step / 2; i < list.Count - step + step / 2; i++)
        {
            // 当前点，取前后各一半，即 step / 2 个点，求平均值作为当前点的值
            newList.Add(list.Skip(i - step / 2).Take(step).Sum() / step);
        }
        // 后面一半加不了
        newList.AddRange(list.Skip(list.Count - (step - step / 2)));
        return newList;
    }

    internal SkiaStrokeDrawContext CreateDrawContext()
    {
        SKPath skPath;
        bool shouldDisposePath;
        if (_isStaticStroke)
        {
            // 静态笔迹，不需要复制，因为不会再更改，不存在线程安全问题
            skPath = Path;
            // 静态笔迹不需要释放，释放了会导致绘制闪退
            shouldDisposePath = false;
        }
        else
        {
            skPath = Path.Clone();
            shouldDisposePath = true;
        }

        return new SkiaStrokeDrawContext(Color, skPath, GetDrawBounds(), shouldDisposePath);
    }

    internal void SetAsStatic()
    {
        _drawBounds = GetDrawBounds();
        _isStaticStroke = true;
    }

    private bool _isStaticStroke;
    private Rect _drawBounds;

    public Rect GetDrawBounds()
    {
        if (_isStaticStroke)
        {
            return _drawBounds;
        }

        return Path.Bounds.ToAvaloniaRect().Expand(Width);
    }
}

readonly record struct SkiaStrokeDrawContext(SKColor Color, SKPath Path, Rect DrawBounds, bool ShouldDisposePath) : IDisposable
{
    public void Dispose()
    {
        if (ShouldDisposePath)
        {
            Path.Dispose();
        }
    }
}

class DynamicStrokeContext
{
    public DynamicStrokeContext(InkingInputArgs lastInputArgs)
    {
        LastInputArgs = lastInputArgs;

        Stroke = new SkiaStroke(InkId.NewId());
    }

    public InkingInputArgs LastInputArgs { get; }

    public int Id => LastInputArgs.Id;

    public SkiaStroke Stroke { get; }
}

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