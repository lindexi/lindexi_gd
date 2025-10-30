using Avalonia;
using Avalonia.Skia;

using DotNetCampus.Inking.Contexts;
using DotNetCampus.Inking.Primitive;
using DotNetCampus.Inking.StrokeRenderers;
using DotNetCampus.Inking.Utils;
using DotNetCampus.Numerics.Geometry;

using SkiaSharp;

using UnoInk.Inking.InkCore;

namespace DotNetCampus.Inking;

public class SkiaStroke : IDisposable
{
    public SkiaStroke(InkId id) : this(id, new SKPath(), ownSkiaPath: true)
    {
    }

    private SkiaStroke(InkId id, SKPath path, bool ownSkiaPath)
    {
        _ownSkiaPath = ownSkiaPath;
        Id = id;
        Path = path;
    }

    /// <summary>
    /// 笔迹渲染器
    /// </summary>
    public ISkiaInkStrokeRenderer? InkStrokeRenderer { get; init; }

    public AvaloniaSkiaInkCanvas? InkCanvas { get; set; }

    public InkId Id { get; }

    public SKPath Path { get; private set; }

    public SKMatrix Transform { get; private set; } = SKMatrix.Identity;

    /// <summary>
    /// 是否拥有 <see cref="Path"/> 的所有权，即需要在释放的使用同步将其释放
    /// </summary>
    private readonly bool _ownSkiaPath;

    /// <summary>
    /// 笔迹颜色
    /// </summary>
    public SKColor Color { get; init; } = AvaloniaSkiaInkCanvasSettings.DefaultInkColor;

    /// <summary>
    /// 笔迹粗细
    /// </summary>
    public float InkThickness { get; init; } = AvaloniaSkiaInkCanvasSettings.DefaultInkThickness;

    internal IReadOnlyList<InkStylusPoint> PointList => _pointList;

    /// <summary>
    /// 是否忽略压感
    /// </summary>
    public bool IgnorePressure { get; init; }

    private readonly List<InkStylusPoint> _pointList = [];

    public void AddPoint(InkStylusPoint point) => AddPoints([point]);

    public void AddPoints(in IEnumerable<InkStylusPoint> points)
    {
        if (_isStaticStroke)
        {
            throw new InvalidOperationException($"禁止修改静态笔迹的点");
        }

        InkStylusPoint? lastPoint = _pointList.Count > 0 ? PointList[^1] : default;
        foreach (InkStylusPoint currentPoint in points)
        {
            InkStylusPoint point = currentPoint;

            if (IgnorePressure)
            {
                point = point with
                {
                    Pressure = InkStylusPoint.DefaultPressure,
                };
            }

            if (lastPoint == point)
            {
                // 如果两个点相同，则丢点
                continue;
            }

            lastPoint = point;

            _pointList.Add(point);
        }

        var pointList = _pointList;
        if (InkCanvas?.Settings.ShouldReCreatePoint is true && pointList.Count > 10)
        {
            pointList = ApplyMeanFilter(pointList);
        }

        RenderInk(pointList);
    }

    private void RenderInk(List<InkStylusPoint> pointList)
    {
        if (InkStrokeRenderer is not null)
        {
            // 如果有传入渲染器，则使用传入的渲染器
            Path = InkStrokeRenderer.RenderInkToPath(pointList, InkThickness);
        }
        else
        {
            if (pointList.Count >= 2)
            {
                var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, InkThickness);

                Path.Reset();
                Path.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
            }
            else if (pointList.Count == 1)
            {
                Path.Reset();
                var stylusPoint = pointList[0];
                float x = (float) stylusPoint.X;
                float y = (float) stylusPoint.Y;
                // 如果是一个点，那就画一个圆。圆的半径就是 笔迹粗细 * 压力 / 2
                // 为什么要除以 2，因为传入的是半径
                var radius = InkThickness * stylusPoint.Pressure / 2;
                Path.AddCircle(x, y, radius);
            }
            else
            {
                // 一个点都没有，那就什么都不画
            }
        }
    }

    public void Dispose()
    {
        if (_ownSkiaPath)
        {
            Path.Dispose();
        }
    }

    public static List<InkStylusPoint> ApplyMeanFilter(List<InkStylusPoint> pointList, int step = 10)
    {
        var xList = ApplyMeanFilter(pointList.Select(t => t.Point.X).ToList(), step);
        var yList = ApplyMeanFilter(pointList.Select(t => t.Point.Y).ToList(), step);

        var newPointList = new List<InkStylusPoint>();
        for (int i = 0; i < xList.Count && i < yList.Count; i++)
        {
            newPointList.Add(new InkStylusPoint(xList[i], yList[i]));
        }

        return newPointList;
    }

    /// <summary>
    /// 滤波算法，细节请看 [WPF 记一个特别简单的点集滤波平滑方法 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18387840 )
    /// </summary>
    /// <param name="list"></param>
    /// <param name="step"></param>
    /// <returns></returns>
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
            // 动态笔迹，需要复制，因为可能会在多个线程中绘制使用和释放
            // 如在 UI 线程加点，修改 Path 内容。与此同时在渲染线程绘制，导致多线程同时访问
            // 为了避免这种情况，复制 Path 解决线程安全问题
            skPath = Path.Clone();
            shouldDisposePath = true;
        }

        return new SkiaStrokeDrawContext(Color, skPath, GetDrawBounds(), Transform, shouldDisposePath);
    }

    internal void SetAsStatic()
    {
        _drawBounds = GetDrawBounds();
        _isStaticStroke = true;
    }

    public static SkiaStroke CreateStaticStroke(InkId id, SKPath path, StylusPointListSpan pointList, SKColor color,
        float inkThickness, bool ownSkiaPath, ISkiaInkStrokeRenderer? inkStrokeRenderer)
    {
        var skiaStroke = new SkiaStroke(id, path, ownSkiaPath)
        {
            Color = color,
            InkThickness = inkThickness,
            InkStrokeRenderer = inkStrokeRenderer,
        };

        skiaStroke._pointList.EnsureCapacity(pointList.Length);
        skiaStroke._pointList.AddRange(pointList.GetEnumerable());
        skiaStroke.SetAsStatic();

        return skiaStroke;
    }

    private bool _isStaticStroke;
    private Rect _drawBounds;

    public Rect GetDrawBounds()
    {
        if (_isStaticStroke)
        {
            return _drawBounds;
        }

        return SkiaSharpExtensions.ToAvaloniaRect(Path.Bounds).ExpandLength(InkThickness);
    }

    public void SetTransform(SKMatrix matrix)
    {
        Transform = matrix;
        InkCanvas?.InvalidateVisual();
    }

    public void ApplyTransform(SimilarityTransformation2D transform)
    {
        for (var i = 0; i < _pointList.Count; i++)
        {
            var point = _pointList[i];
            _pointList[i] = new InkStylusPoint(transform.Transform(point.Point), point.Pressure);
        }

        Path = new SKPath();

        if (_pointList.Count > 2)
        {
            var outlinePointList = SimpleInkRender.GetOutlinePointList(_pointList, InkThickness);

            Path.Reset();
            Path.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        }

        Transform = SKMatrix.Identity;
        _drawBounds = SkiaSharpExtensions.ToAvaloniaRect(Path.Bounds).ExpandLength(InkThickness);

        InkCanvas?.InvalidateVisual();
    }

    public void EnsureIsStaticStroke()
    {
        if (!_isStaticStroke)
        {
            throw new InvalidOperationException();
        }
    }
}