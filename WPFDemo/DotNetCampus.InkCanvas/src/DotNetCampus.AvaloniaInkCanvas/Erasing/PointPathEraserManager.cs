using Avalonia.Skia;

using DotNetCampus.Inking.Contexts;
using DotNetCampus.Inking.Primitive;
using DotNetCampus.Inking.Utils;
using DotNetCampus.Numerics;
using DotNetCampus.Numerics.Geometry;

using SkiaSharp;

using System.Diagnostics;

using UnoInk.Inking.InkCore;

using Point = DotNetCampus.Numerics.Geometry.Point2D;
using Point2D = DotNetCampus.Numerics.Geometry.Point2D;
using Rect = DotNetCampus.Numerics.Geometry.Rect2D;

namespace DotNetCampus.Inking.Erasing;

/// <summary>
/// 点擦路径擦除
/// </summary>
class PointPathEraserManager
{
    private bool _isErasing;

    public void StartEraserPointPath(IReadOnlyList<SkiaStroke> staticStrokeList)
    {
        Debug.Assert(_isErasing == false, $"开始橡皮擦时，开始橡皮擦的 {nameof(_isErasing)} 字段状态一定为 false 值");
        Debug.Assert(WorkList.Count == 0, "橡皮擦计算开始的时候，必然此时没有任何笔迹正在被橡皮擦工作中");
        // 兜底代码，确保 WorkList 为空，防止重复加入导致进入诡异的逻辑
        WorkList.Clear();

        _isErasing = true;

        WorkList.EnsureCapacity(staticStrokeList.Count);
        var workList = WorkList;
        foreach (var skiaStrokeSynchronizer in staticStrokeList)
        {
            workList.Add(new InkInfoForEraserPointPath(skiaStrokeSynchronizer));
        }
    }

    private List<InkInfoForEraserPointPath> WorkList { get; } = [];

    private readonly List<SubInkInfoForEraserPointPath> _cacheList = [];

    public void Move(Rect2D rect) => Move(new RotatedRect(new Point2D(rect.X, rect.Y), new Size2D(rect.Width, rect.Height), AngularMeasure.Zero));

    public void Move(RotatedRect rotatedEraserRect)
    {
        var eraserBoundingBox = rotatedEraserRect.GetBoundingBox();
        var transformation = AffineTransformation2D.Identity.RotateAt(-rotatedEraserRect.Rotate, rotatedEraserRect.Location).Translate(-rotatedEraserRect.Location.ToVector());
        var eraserBoundingBoxRect2D = new Rect2D(eraserBoundingBox.MinX, eraserBoundingBox.MinY, eraserBoundingBox.Width, eraserBoundingBox.Height);

        foreach (InkInfoForEraserPointPath inkInfoForEraserPointPath in WorkList)
        {
            _cacheList.Clear();

            // 擦点的核心逻辑
            foreach (SubInkInfoForEraserPointPath pointPath in inkInfoForEraserPointPath.SubInkInfoList)
            {
                var bounds = pointPath.CacheBounds;
                if (!bounds.IntersectsWith(eraserBoundingBoxRect2D))
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

                    if (
                        // 1. 短路过滤：快速判断目标点是否在旋转矩形的外接边框内（这可以过滤掉板书中的绝大部分点）。
                        eraserBoundingBox.Contains(point)
                        // 2. 精确判断：判断目标点是否在旋转矩形内。
                        //    2.1. 将目标点转换到旋转矩形的坐标系中；
                        && transformation.Transform(point) is { X: >= 0, Y: >= 0 } transformedPoint
                        //    2.2. 判断变换后的点是否在矩形内。
                        && transformedPoint.X <= rotatedEraserRect.Size.Width
                        && transformedPoint.Y <= rotatedEraserRect.Size.Height)
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

                // 这里的 start 是相对于 pointPath.PointPath 的，而不是相对于当前的 pointPath.PointListSpan 的。因此 start 为 0 不代表就是当前的 pointPath 的起点，而应该是 start == pointPath.PointListSpan.Start 才是代表起点和 pointPath 相同
                if (start == pointPath.PointListSpan.Start && length == pointPath.PointListSpan.Length)
                {
                    // 短路代码，表示这条笔迹一个点都没被擦掉
                    _cacheList.Add(pointPath);
                }
                else
                {
                    if (start != -1)
                    {
                        // 截断
                        _cacheList.Add(pointPath.Sub(start, length));
                    }

                    // 截断最后需要将原来释放掉
                    pointPath.Dispose();
                }
            }

            inkInfoForEraserPointPath.SubInkInfoList.Clear();
            inkInfoForEraserPointPath.SubInkInfoList.AddRange(_cacheList);

#if DEBUG
            foreach (var subInkInfoForEraserPointPath in _cacheList)
            {
                if (subInkInfoForEraserPointPath.IsDisposed)
                {
                    Debugger.Break();
                }
            }
#endif

            _cacheList.Clear();
        }
    }

    public PointPathEraserResult Finish()
    {
        _isErasing = false;
        var count = WorkList.Sum(t => t.SubInkInfoList.Count);
        var erasingSkiaStrokeList = new List<ErasedSkiaStroke>(count);

        foreach (var inkInfoForEraserPointPath in WorkList)
        {
            var originSkiaStroke = inkInfoForEraserPointPath.OriginSkiaStroke;

            IReadOnlyList<SkiaStroke>? newStrokeList;
            if (inkInfoForEraserPointPath.SubInkInfoList.Count == 0)
            {
                // 笔迹完全被擦掉了
                newStrokeList = Array.Empty<SkiaStroke>();
            }
            else if (inkInfoForEraserPointPath.IsErased)
            {
                var strokeList = new List<SkiaStroke>(inkInfoForEraserPointPath.SubInkInfoList.Count);
                newStrokeList = strokeList;

                foreach (var subInkInfoForEraserPointPath in inkInfoForEraserPointPath.SubInkInfoList)
                {
                    var subSpan = subInkInfoForEraserPointPath.PointListSpan;
                    var pointList = new StylusPointListSpan(originSkiaStroke.PointList, subSpan.Start, subSpan.Length);

                    var skPath = subInkInfoForEraserPointPath.CachePath ?? ToPath(subInkInfoForEraserPointPath);
                    // 已经从 CachePath 取出，不能再有原来的引用，生怕被释放
                    subInkInfoForEraserPointPath.CachePath = null;

                    var skiaStroke = SkiaStroke.CreateStaticStroke(InkId.NewId(), skPath, pointList, originSkiaStroke.Color,
                        originSkiaStroke.InkThickness, ownSkiaPath: true, originSkiaStroke.InkStrokeRenderer);
                    strokeList.Add(skiaStroke);
                }
            }
            else
            {
                // 没被擦的笔迹依然可以使用原来的笔迹，设计上配置 newStrokeList 为空，减少对象的创建
                // 满屏幕的笔迹，然后只擦掉一个笔迹，如果没有被擦掉的笔迹也创建 List 那将会是一个很大的开销
                newStrokeList = null;
            }

            erasingSkiaStrokeList.Add(new ErasedSkiaStroke(originSkiaStroke, newStrokeList, inkInfoForEraserPointPath.IsErased));
        }

        WorkList.Clear();
        var result = new PointPathEraserResult(erasingSkiaStrokeList);
        return result;
    }

    /// <summary>
    /// 获取渲染内容
    /// </summary>
    /// <returns></returns>
    /// 为什么获取渲染内容需要在准备渲染时才获取，而不是在擦的过程中计算？ 原因是机器设备性能太差，擦的过程的进入次数会比渲染次数更多，且在插的过程中计算出来的结果没有被实际使用到，于是不如就在准备渲染的时候计算，如此可以稍微提升一些性能
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
                    if (subInkInfoForEraserPointPath.IsDisposed)
                    {
                        throw new ObjectDisposedException($"当前所使用的 SubInkInfoForEraserPointPath 已经被释放了，橡皮擦状态不正常");
                    }

                    subInkInfoForEraserPointPath.CachePath ??= ToPath(subInkInfoForEraserPointPath);
                    // 为什么需要复制一个？原因是接下来的渲染是交给 Avalonia 的渲染线程上，释放时机不固定。原本的在 UI 线程上的 CachePath 的释放时机是这条笔迹被擦到的时候释放，也不能和渲染线程统一，只好进行拷贝一次
                    var skPath = subInkInfoForEraserPointPath.CachePath.Clone();

                    result.Add(new SkiaStrokeDrawContext(originSkiaStroke.Color, skPath, skPath.Bounds.ToAvaloniaRect(), SKMatrix.Identity, ShouldDisposePath: true));
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
        // 对于 WPF 注入的渲染器，只要大于一个点就可以开始渲染了
        if (subSpan.Length > 0)
        {
            var pointList = new StylusPointListSpan(originSkiaStroke.PointList, subSpan.Start, subSpan.Length);

            if (originSkiaStroke.InkStrokeRenderer is { } inkStrokeRenderer)
            {
                return inkStrokeRenderer.RenderInkToPath(pointList.ToReadOnlyList(), originSkiaStroke.InkThickness);
            }

            if (subSpan.Length > 2)
            {
                var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList.ToReadOnlyList(), originSkiaStroke.InkThickness);

                var skPath = new SKPath();
                skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
                return skPath;
            }
        }

        return new SKPath();
    }

    /// <summary>
    /// 橡皮擦点擦过程中用到的笔迹信息
    /// </summary>
    /// 用于中间计算使用
    class InkInfoForEraserPointPath
    {
        public InkInfoForEraserPointPath(SkiaStroke originSkiaStroke)
        {
            OriginSkiaStroke = originSkiaStroke;

            SubInkInfoList = new List<SubInkInfoForEraserPointPath>();

            var subInk = new SubInkInfoForEraserPointPath(new PointListSpan(0, originSkiaStroke.PointList.Count), this);
            if (originSkiaStroke.Path is { } skPath)
            {
                subInk.CacheBounds = skPath.Bounds.ToRect2D();
            }

            SubInkInfoList.Add(subInk);

            PointList = new Point2D[OriginSkiaStroke.PointList.Count];
            for (var i = 0; i < OriginSkiaStroke.PointList.Count; i++)
            {
                PointList[i] = OriginSkiaStroke.PointList[i].Point;
            }
        }

        public SkiaStroke OriginSkiaStroke { get; }

        /// <summary>
        /// 所有实际带的点
        /// </summary>
        /// 比 <see cref="InkStylusPoint"/> 结构体小，如此可以提升遍历性能
        public Point2D[] PointList { get; }

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
    class SubInkInfoForEraserPointPath : IDisposable
    {
        public SubInkInfoForEraserPointPath(PointListSpan pointListSpan, InkInfoForEraserPointPath pointPath)
        {
            PointListSpan = pointListSpan;
            PointPath = pointPath;
        }

        public SKPath? CachePath { get; set; }

        public InkInfoForEraserPointPath PointPath { get; }

        public Rect2D CacheBounds
        {
            get
            {
                if (_cacheBounds == null)
                {
                    var span = PointPath.PointList.AsSpan(PointListSpan.Start, PointListSpan.Length);
                    Rect2D bounds = new Rect2D();

                    if (span.Length > 0)
                    {
                        bounds = new Rect2D(span[0].X, span[0].Y, 0, 0);
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

        private Rect2D? _cacheBounds;

        public PointListSpan PointListSpan { get; }

        public SubInkInfoForEraserPointPath Sub(int start, int length)
        {
            return new SubInkInfoForEraserPointPath(new PointListSpan(start, length), PointPath)
            {
                _cacheBounds = null
            };
        }

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            IsDisposed = true;
            CachePath?.Dispose();
            CachePath = null;
        }
    }

    readonly record struct PointListSpan(int Start, int Length);
}