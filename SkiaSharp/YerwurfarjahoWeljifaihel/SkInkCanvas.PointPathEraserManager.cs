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

        #endregion
    }
}