using System.Linq;

using Avalonia.Skia;

using Microsoft.Maui.Graphics;

using WejallkachawDadeawejearhuce.Inking.Contexts;
using WejallkachawDadeawejearhuce.Inking.Primitive;
using WejallkachawDadeawejearhuce.Inking.Utils;

using SkiaSharp;

using UnoInk.Inking.InkCore;

namespace WejallkachawDadeawejearhuce.Inking.Erasing;

class PointPathEraserManager
{
    public void StartEraserPointPath(IReadOnlyList<SkiaStroke> staticStrokeList)
    {
        WorkList.EnsureCapacity(staticStrokeList.Count);
        var workList = WorkList;
        foreach (var skiaStrokeSynchronizer in staticStrokeList)
        {
            workList.Add(new InkInfoForEraserPointPath(skiaStrokeSynchronizer));
        }
    }

    private List<InkInfoForEraserPointPath> WorkList { get; } = [];

    private readonly List<SubInkInfoForEraserPointPath> _cacheList = [];

    public void Move(Rect rect)
    {
        foreach (InkInfoForEraserPointPath inkInfoForEraserPointPath in WorkList)
        {
            _cacheList.Clear();

            foreach (SubInkInfoForEraserPointPath pointPath in inkInfoForEraserPointPath.SubInkInfoList)
            {
                var bounds = pointPath.CacheBounds;
                if (!bounds.IntersectsWith(rect))
                {
                    _cacheList.Add(pointPath);
                    continue;
                }

                var span = pointPath.PointListSpan;
                var start = -1;
                var length = 0;

                for (int i = 0; i < span.Length; i++)
                {
                    var index = span.Start + i;
                    var point = inkInfoForEraserPointPath.PointList[index];

                    //var point = inkInfoForEraserPointPath.StrokeSynchronizer.StylusPoints[index].Point;
                    //_pointCount++;

                    if (rect.Contains(point))
                    {
                        if (start != -1)
                        {
                            // 截断
                            _cacheList.Add(pointPath.Sub(start, length));
                        }

                        start = -1;
                        length = 0;
                    }
                    else
                    {
                        if (start == -1)
                        {
                            start = index;
                            length = 1;
                        }
                        else
                        {
                            length++;
                        }
                    }
                }

                if (start != -1)
                {
                    // 截断
                    _cacheList.Add(pointPath.Sub(start, length));
                }
            }

            inkInfoForEraserPointPath.SubInkInfoList.Clear();
            inkInfoForEraserPointPath.SubInkInfoList.AddRange(_cacheList);
            _cacheList.Clear();
        }
    }

    public PointPathEraserResult Finish()
    {
        var count = WorkList.Sum(t => t.SubInkInfoList.Count);
        var erasingSkiaStrokeList = new List<ErasingSkiaStroke>(count);

        foreach (var inkInfoForEraserPointPath in WorkList)
        {
            var originSkiaStroke = inkInfoForEraserPointPath.OriginSkiaStroke;
            var newStrokeList = new List<SkiaStroke>(inkInfoForEraserPointPath.SubInkInfoList.Count);

            foreach (var subInkInfoForEraserPointPath in inkInfoForEraserPointPath.SubInkInfoList)
            {
                var subSpan = subInkInfoForEraserPointPath.PointListSpan;
                var pointList = new StylusPointListSpan(originSkiaStroke.PointList, subSpan.Start, subSpan.Length);

                var skPath = ToPath(subInkInfoForEraserPointPath);

                var skiaStroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), skPath, pointList, originSkiaStroke.Color,
                    originSkiaStroke.InkThickness);
                newStrokeList.Add(skiaStroke);
                //result.Add(new SkiaStrokeDrawContext(subInkInfoForEraserPointPath.PointPath.OriginSkiaStroke.Color, skPath, skPath.Bounds.ToAvaloniaRect(), ShouldDisposePath: true));
            }

            erasingSkiaStrokeList.Add(new ErasingSkiaStroke(originSkiaStroke, newStrokeList));
        }

        WorkList.Clear();
        var result = new PointPathEraserResult(erasingSkiaStrokeList);
        return result;
    }

    public IReadOnlyList<SkiaStrokeDrawContext> GetDrawContextList()
    {
        var count = WorkList.Sum(t => t.SubInkInfoList.Count);
        var result = new List<SkiaStrokeDrawContext>(count);

        foreach (var inkInfoForEraserPointPath in WorkList)
        {
            var originSkiaStroke = inkInfoForEraserPointPath.OriginSkiaStroke;
            if (inkInfoForEraserPointPath.IsErased)
            {
                // 被擦掉的笔迹，就需要逐个笔迹计算
                foreach (var subInkInfoForEraserPointPath in inkInfoForEraserPointPath.SubInkInfoList)
                {
                    var skPath = ToPath(subInkInfoForEraserPointPath);

                    result.Add(new SkiaStrokeDrawContext(originSkiaStroke.Color, skPath, skPath.Bounds.ToAvaloniaRect(), ShouldDisposePath: true));
                }
            }
            else
            {
                // 没被擦的笔迹依然可以使用静态笔迹提升性能
#if DEBUG
                originSkiaStroke.EnsureIsStaticStroke();
#endif
                result.Add(originSkiaStroke.CreateDrawContext());
            }
        }

        return result;
    }

    private static SKPath ToPath(SubInkInfoForEraserPointPath subInkInfoForEraserPointPath)
    {
        SkiaStroke originSkiaStroke = subInkInfoForEraserPointPath.PointPath.OriginSkiaStroke;

        var subSpan = subInkInfoForEraserPointPath.PointListSpan;
        var skPath = new SKPath();
        if (subSpan.Length > 2)
        {
            var pointList = new StylusPointListSpan(originSkiaStroke.PointList, subSpan.Start, subSpan.Length);

            var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList.ToReadOnlyList(), originSkiaStroke.InkThickness);

            skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        }

        return skPath;
    }

    class InkInfoForEraserPointPath
    {
        public InkInfoForEraserPointPath(SkiaStroke originSkiaStroke)
        {
            OriginSkiaStroke = originSkiaStroke;

            SubInkInfoList = new List<SubInkInfoForEraserPointPath>();

            var subInk = new SubInkInfoForEraserPointPath(new PointListSpan(0, originSkiaStroke.PointList.Count), this);
            if (originSkiaStroke.Path is { } skPath)
            {
                subInk.CacheBounds = skPath.Bounds.ToMauiRect();
            }

            SubInkInfoList.Add(subInk);

            PointList = new Point[OriginSkiaStroke.PointList.Count];
            for (var i = 0; i < OriginSkiaStroke.PointList.Count; i++)
            {
                PointList[i] = OriginSkiaStroke.PointList[i].Point;
            }
        }

        public SkiaStroke OriginSkiaStroke { get; }

        /// <summary>
        /// 所有实际带的点
        /// </summary>
        /// 比 <see cref="StylusPoint"/> 结构体小，如此可以提升遍历性能
        public Point[] PointList { get; }

        /// <summary>
        /// 拆分出来的笔迹
        /// </summary>
        /// 默认会有一条笔迹，就是原始的
        public List<SubInkInfoForEraserPointPath> SubInkInfoList { get; }

        /// <summary>
        /// 是否被擦到了
        /// </summary>
        public bool IsErased
        {
            get
            {
                if (SubInkInfoList.Count == 1)
                {
                    var subInk = SubInkInfoList[0];
                    if (subInk.PointListSpan.Start == 0 && subInk.PointListSpan.Length == PointList.Length)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    /// <summary>
    /// 被橡皮擦拆分的子笔迹信息
    /// </summary>
    class SubInkInfoForEraserPointPath
    {
        public SubInkInfoForEraserPointPath(PointListSpan pointListSpan, InkInfoForEraserPointPath pointPath)
        {
            PointListSpan = pointListSpan;
            PointPath = pointPath;
        }

        public InkInfoForEraserPointPath PointPath { get; }

        public Rect CacheBounds
        {
            get
            {
                if (_cacheBounds == null)
                {
                    var span = PointPath.PointList.AsSpan(PointListSpan.Start, PointListSpan.Length);
                    Rect bounds = Rect.Zero;

                    if (span.Length > 0)
                    {
                        bounds = new Rect(span[0].X, span[0].Y, 0, 0);
                    }

                    for (int i = 1; i < span.Length; i++)
                    {
                        bounds = bounds.Union(span[i]);
                    }

                    _cacheBounds = bounds;
                }

                return _cacheBounds.Value;
            }
            set => _cacheBounds = value;
        }

        private Rect? _cacheBounds;

        public PointListSpan PointListSpan { get; }

        public SubInkInfoForEraserPointPath Sub(int start, int length)
        {
            return new SubInkInfoForEraserPointPath(new PointListSpan(start, length), PointPath)
            {
                _cacheBounds = null
            };
        }
    }

    readonly record struct PointListSpan(int Start, int Length);
}