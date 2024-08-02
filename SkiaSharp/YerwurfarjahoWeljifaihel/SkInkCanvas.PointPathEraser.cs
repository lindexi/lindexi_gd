using SkiaInkCore.Interactives;
using SkiaInkCore.Primitive;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BujeeberehemnaNurgacolarje;
using SkiaInkCore;
using SkiaInkCore.Diagnostics;
using SkiaInkCore.Utils;
using System.Threading;

namespace ReewheaberekaiNayweelehe;

partial class SkInkCanvas
{
    class PointPathEraserManager
    {
        public PointPathEraserManager(SkInkCanvas skInkCanvas)
        {
            _skInkCanvas = skInkCanvas;
        }

        private readonly SkInkCanvas _skInkCanvas;

        public void StartEraserPointPath()
        {
            var workList = new List<InkInfoForEraserPointPath>(_skInkCanvas.StaticInkInfoList.Count);

            foreach (var skiaStrokeSynchronizer in _skInkCanvas.StaticInkInfoList)
            {
                workList.Add(new InkInfoForEraserPointPath(skiaStrokeSynchronizer));
            }

            WorkList = workList;
        }

        private List<InkInfoForEraserPointPath> WorkList { get; set; } = null!;

        private Stopwatch _stopwatch = new Stopwatch();
        private double _totalTime;
        private int _count;
        //private int _pointCount;

        public void Move(Rect rect)
        {
            _stopwatch.Restart();

            //Parallel.ForEach(WorkList, (inkInfoForEraserPointPath, status) =>
            //{
            //    foreach (ErasingSubInkInfoForEraserPointPath pointPath in inkInfoForEraserPointPath.SubInkInfoList)
            //    {
            //        var span = pointPath.StylusPointListSpan;

            //        for (int i = 0; i < span.Length; i++)
            //        {
            //            var index = span.Start + i;
            //            StylusPoint stylusPoint = inkInfoForEraserPointPath.StrokeSynchronizer.StylusPoints[index];
            //            var point = stylusPoint.Point;

            //            if (rect.Contains(point))
            //            {

            //            }
            //        }
            //    }
            //});

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


            var staticInkInfoList = _skInkCanvas.StaticInkInfoList;
            staticInkInfoList.Clear();

            foreach (InkInfoForEraserPointPath inkInfoForEraserPointPath in WorkList)
            {
                if (inkInfoForEraserPointPath.SubInkInfoList.Count == 1)
                {
                    var span = inkInfoForEraserPointPath.SubInkInfoList[0].PointListSpan;
                    if (span.Start == 0 && span.Length == inkInfoForEraserPointPath.PointList.Length)
                    {
                        staticInkInfoList.Add(inkInfoForEraserPointPath.StrokeSynchronizer);

                        continue;
                    }
                }

                //inkInfoForEraserPointPath.StrokeSynchronizer.InkStrokePath?.Dispose();

                foreach (SubInkInfoForEraserPointPath subInkInfo in inkInfoForEraserPointPath.SubInkInfoList)
                {
                    var span = subInkInfo.PointListSpan;

                    if (span.Length <= 2)
                    {
                        // 不能创建笔迹了
                        continue;
                    }

                    var newList =
                        inkInfoForEraserPointPath.StrokeSynchronizer.StylusPoints.GetRange(span.Start, span.Length);

                    var outlinePointList = SimpleInkRender.GetOutlinePointList(newList, inkInfoForEraserPointPath.StrokeSynchronizer.StrokeInkThickness);
                    var skPath = new SKPath() { FillType = SKPathFillType.Winding };
                    skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());

                    var skiaStrokeSynchronizer = inkInfoForEraserPointPath.StrokeSynchronizer with
                    {
                        InkId = InkId.NewId(),
                        InkStrokePath = skPath,
                        StylusPoints = newList
                    };

                    staticInkInfoList.Add(skiaStrokeSynchronizer);
                }
            }

            _stopwatch.Stop();
            _totalTime += _stopwatch.Elapsed.TotalMilliseconds;
            _count++;

            if (_count > 100)
            {
                StaticDebugLogger.WriteLine($"[PointPathEraserManager] Move 平均耗时 {_totalTime / _count}");

                _totalTime = 0;
                _count = 0;
            }
        }

        #region 辅助类型

        /// <summary>
        /// 对 <see cref="SkiaStrokeSynchronizer"/> 封装的类型，用于提升性能
        /// </summary>
        class InkInfoForEraserPointPath
        {
            public InkInfoForEraserPointPath(SkiaStrokeSynchronizer strokeSynchronizer)
            {
                StrokeSynchronizer = strokeSynchronizer;
                SubInkInfoList = new List<SubInkInfoForEraserPointPath>();

                var subInk = new SubInkInfoForEraserPointPath(new PointListSpan(0, strokeSynchronizer.StylusPoints.Count), this);
                if (strokeSynchronizer.InkStrokePath is { } skPath)
                {
                    subInk.CacheBounds = skPath.Bounds.ToMauiRect();
                }

                SubInkInfoList.Add(subInk);

                PointList = new Point[StrokeSynchronizer.StylusPoints.Count];
                for (var i = 0; i < StrokeSynchronizer.StylusPoints.Count; i++)
                {
                    PointList[i] = StrokeSynchronizer.StylusPoints[i].Point;
                }
            }

            /// <summary>
            /// 所有实际带的点
            /// </summary>
            /// 比 <see cref="StylusPoint"/> 结构体小，如此可以提升遍历性能
            public Point[] PointList { get; }

            public SkiaStrokeSynchronizer StrokeSynchronizer { get; set; }

            /// <summary>
            /// 拆分出来的笔迹
            /// </summary>
            /// 默认会有一条笔迹，就是原始的
            public List<SubInkInfoForEraserPointPath> SubInkInfoList { get; }
        }

        private readonly List<SubInkInfoForEraserPointPath> _cacheList = new List<SubInkInfoForEraserPointPath>();

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
                            var rect2D = new Rect(span[i].X, span[i].Y, 0, 0);
                            bounds = bounds.Union(rect2D);
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

        #endregion
    }
}

partial class SkInkCanvas
{
    private void StartEraserPointPath()
    {
        _isEraserPointPathStart = true;
        _pointPathEraserManager = new PointPathEraserManager(this);
        _pointPathEraserManager.StartEraserPointPath();
    }

    private PointPathEraserManager? _pointPathEraserManager;

    private bool _isEraserPointPathStart;

    private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

    private void MoveEraserPointPath(InkingModeInputArgs info, double width, double height)
    {
        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

        if (!_isEraserPointPathStart)
        {
            StartEraserPointPath();
        }

        Debug.Assert(_pointPathEraserManager != null);

        var point = info.StylusPoint.Point;
        var x = (float) point.X;
        var y = (float) point.Y;

        // 变换为左上角
        x -= (float) width / 2;
        y -= (float) height / 2;

        Task.Run(async () =>
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                _pointPathEraserManager.Move(new Rect(x, y, width, height));
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        });

        var skRect = new SKRect(x, y, (float) (x + width), (float) (y + height));
        // 比擦掉的范围更大的范围，用于持续更新
        var expandRect = RectExtension.ExpandSKRect(skRect, 10);
        if (_lastEraserRenderBounds is not null)
        {
            // 理论上此时需要从原先的拷贝覆盖，否则将不能清掉上次的橡皮擦内容
            // 重新绘制 _origin 的，用于修复清理的问题
            // 为什么其他的模式不需要？原因是其他的模式的裁剪是全部的
            // 用于修复橡皮擦图标没有删除
            expandRect.Union(_lastEraserRenderBounds.Value.ToSkRect());
        }
        expandRect = LimitRectInAppBitmapRect(expandRect);

        //ApplicationDrawingSkBitmap.ReplacePixels(_originBackground, SKRectI.Ceiling(expandRect));





        if (EraserPath is null)
        {
            EraserPath = new SKPath();
        }
        else
        {
            EraserPath.Reset();
        }
        EraserPath.AddRect(new SKRect(0, 0, _originBackground.Width, _originBackground.Height));
        // 几何裁剪本身无视顺序，因此先处理当前点再处理之前的点也是正确的
        using var skRoundRect = new SKPath();
        skRoundRect.AddRoundRect(skRect, 5, 5);
        EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

        canvas.Clear();
        canvas.Save();
        canvas.ClipPath(EraserPath, antialias: true);
        canvas.DrawBitmap(_originBackground, 0, 0);
        canvas.Restore();

        canvas.Flush();

        //重新更新 _originBackground 的内容，需要在画出橡皮擦之前
        UpdateOriginBackground();

        // 画出橡皮擦
        canvas.Save();
        canvas.Translate(x, y);
        EraserView.DrawEraserView(canvas, (int) width, (int) height);
        canvas.Restore();

        // 更新范围
        var addition = 20;

        var currentEraserRenderBounds = new Rect(x - addition, y - addition, width + addition * 2, height + addition * 2);
        currentEraserRenderBounds = LimitRectInAppBitmapRect(currentEraserRenderBounds);

        var rect = currentEraserRenderBounds;
        if (_lastEraserRenderBounds != null)
        {
            // 将上次的绘制范围进行重新绘制，防止出现橡皮擦图标
            rect = rect.Union(_lastEraserRenderBounds.Value);
        }
        rect = LimitRectInAppBitmapRect(rect);
        _lastEraserRenderBounds = currentEraserRenderBounds;

        RenderBoundsChanged?.Invoke(this, rect);
    }

    private async void CleanEraserPointPath()
    {
        _isEraserPointPathStart = false;

        if (_lastEraserRenderBounds is null)
        {
            return;
        }

        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

        var rect = _lastEraserRenderBounds.Value;
        rect = LimitRectInAppBitmapRect(rect);
        ApplicationDrawingSkBitmap.ReplacePixels(_originBackground, SKRectI.Ceiling(rect.ToSkRect()));

        RenderBoundsChanged?.Invoke(this, rect);

        await Task.Delay(TimeSpan.FromSeconds(1));
        RequestDispatcher?.Invoke(this, () =>
        {
            DrawAllInk();
            RenderBoundsChanged?.Invoke(this, new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
        });
    }

    public event EventHandler<Action>? RequestDispatcher;
}
