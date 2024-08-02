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
                foreach (ErasingSubInkInfoForEraserPointPath pointPath in inkInfoForEraserPointPath.SubInkInfoList)
                {
                    var bounds = pointPath.CacheBounds;
                    if (!bounds.IntersectsWith(rect))
                    {
                        continue;
                    }

                    var span = pointPath.StylusPointListSpan;

                    for (int i = 0; i < span.Length; i++)
                    {
                        var index = span.Start + i;
                        var point = inkInfoForEraserPointPath.PointList[index];

                        //var point = inkInfoForEraserPointPath.StrokeSynchronizer.StylusPoints[index].Point;
                        //_pointCount++;

                        if (rect.Contains(point))
                        {

                        }
                    }
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

        class InkInfoForEraserPointPath
        {
            public InkInfoForEraserPointPath(SkiaStrokeSynchronizer strokeSynchronizer)
            {
                StrokeSynchronizer = strokeSynchronizer;
                SubInkInfoList = new List<ErasingSubInkInfoForEraserPointPath>();

                var subInk = new ErasingSubInkInfoForEraserPointPath(new StylusPointListSpan(0, strokeSynchronizer.StylusPoints.Count), this);
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

            public Point[] PointList { get; }

            public SkiaStrokeSynchronizer StrokeSynchronizer { get; set; }
            public List<ErasingSubInkInfoForEraserPointPath> SubInkInfoList { get; }
        }

        /// <summary>
        /// 被橡皮擦拆分的子笔迹信息
        /// </summary>
        class ErasingSubInkInfoForEraserPointPath
        {
            public ErasingSubInkInfoForEraserPointPath(StylusPointListSpan stylusPointListSpan, InkInfoForEraserPointPath pointPath)
            {
                StylusPointListSpan = stylusPointListSpan;
                PointPath = pointPath;
            }

            public InkInfoForEraserPointPath PointPath { get; }

            public Rect CacheBounds
            {
                get
                {
                    if (_cacheBounds == null)
                    {
                        var span = PointPath.PointList.AsSpan(StylusPointListSpan.Start, StylusPointListSpan.Length);
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

            public StylusPointListSpan StylusPointListSpan { get; }
        }

        readonly record struct StylusPointListSpan(int Start, int Length);

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

        _pointPathEraserManager.Move(new Rect(x, y, width, height));

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

        ApplicationDrawingSkBitmap.ReplacePixels(_originBackground, SKRectI.Ceiling(expandRect));





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

    private void CleanEraserPointPath()
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
    }
}
