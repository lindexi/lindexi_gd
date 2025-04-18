using System.Linq;

using Avalonia;
using Avalonia.Skia;

using WejallkachawDadeawejearhuce.Inking.Contexts;
using WejallkachawDadeawejearhuce.Inking.Primitive;
using WejallkachawDadeawejearhuce.Inking.Utils;

using SkiaSharp;

using UnoInk.Inking.InkCore;

namespace WejallkachawDadeawejearhuce.Inking;

public class SkiaStroke : IDisposable
{
    public SkiaStroke(InkId id) : this(id, new SKPath())
    {
    }

    private SkiaStroke(InkId id, SKPath path)
    {
        Id = id;
        Path = path;
    }

    public InkId Id { get; }

    public SKPath Path { get; }

    public SKColor Color { get; init; } = SKColors.Red;
    public float InkThickness { get; init; } = 20;

    public IReadOnlyList<StylusPoint> PointList => _pointList;
    private readonly List<StylusPoint> _pointList = [];

    /// <summary>
    /// 是否需要重新创建笔迹点，采用平滑滤波算法
    /// </summary>
    public static bool ShouldReCreatePoint { get; set; } = false;

    public void AddPoint(StylusPoint point)
    {
        if (_isStaticStroke)
        {
            throw new InvalidOperationException($"禁止修改静态笔迹的点");
        }

        if (_pointList.Count > 0)
        {
            var lastPoint = PointList[^1];
            if (lastPoint == point)
            {
                // 如果两个点相同，则丢点
                return;
            }
        }

        _pointList.Add(point);

        var pointList = _pointList;
        if (ShouldReCreatePoint && pointList.Count > 10)
        {
            pointList = ApplyMeanFilter(pointList);
        }

        if (pointList.Count > 2)
        {
            var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, InkThickness);

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

    public static SkiaStroke CreateStaticStroke(InkId id, SKPath path, StylusPointListSpan pointList, SKColor color, float inkThickness)
    {
        var skiaStroke = new SkiaStroke(id, path)
        {
            Color = color,
            InkThickness = inkThickness,
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

        return Path.Bounds.ToAvaloniaRect().Expand(InkThickness);
    }

    public void EnsureIsStaticStroke()
    {
        if (!_isStaticStroke)
        {
            throw new InvalidOperationException();
        }
    }
}