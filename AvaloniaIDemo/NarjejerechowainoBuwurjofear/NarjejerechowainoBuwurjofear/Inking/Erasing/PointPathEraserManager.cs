using System.Linq;

using Avalonia.Skia;

using Microsoft.Maui.Graphics;

using SkiaSharp;

using UnoInk.Inking.InkCore;

namespace NarjejerechowainoBuwurjofear.Inking.Erasing;

record PointPathEraserResult();

//readonly record struct ErasingSkiaStrokeDrawContext(SKColor Color, SKPath Path,)
//{

//}

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
        var result = new PointPathEraserResult();
        WorkList.Clear();
        return result;
    }

    public IReadOnlyList<SkiaStrokeDrawContext> GetDrawContextList()
    {
        var count = WorkList.Sum(t => t.SubInkInfoList.Count);
        var result = new List<SkiaStrokeDrawContext>(count);

        foreach (var inkInfoForEraserPointPath in WorkList)
        {
            foreach (var subInkInfoForEraserPointPath in inkInfoForEraserPointPath.SubInkInfoList)
            {
                SkiaStroke originSkiaStroke = inkInfoForEraserPointPath.OriginSkiaStroke;

                var subSpan = subInkInfoForEraserPointPath.PointListSpan;
                var skPath = new SKPath();

                if (subSpan.Length > 2)
                {
                    var pointList =
                        inkInfoForEraserPointPath.OriginSkiaStroke.PointList.GetRange(subSpan.Start, subSpan.Length);

                    var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, originSkiaStroke.Width);

                    skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
                }

                result.Add(new SkiaStrokeDrawContext(inkInfoForEraserPointPath.OriginSkiaStroke.Color, skPath, skPath.Bounds.ToAvaloniaRect(), ShouldDisposePath: true));
            }
        }

        return result;
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
                subInk.CacheBounds = skPath.Bounds.ToAvaloniaRect().ToMauiRect();
            }

            SubInkInfoList.Add(subInk);

            PointList = new Point[StrokeSynchronizer.PointList.Count];
            for (var i = 0; i < StrokeSynchronizer.PointList.Count; i++)
            {
                PointList[i] = StrokeSynchronizer.PointList[i].Point;
            }
        }

        public SkiaStroke StrokeSynchronizer => OriginSkiaStroke;
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