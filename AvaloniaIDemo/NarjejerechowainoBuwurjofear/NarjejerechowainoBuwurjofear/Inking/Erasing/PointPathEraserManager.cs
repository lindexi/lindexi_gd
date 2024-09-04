using Microsoft.Maui.Graphics;
using UnoInk.Inking.InkCore;

namespace NarjejerechowainoBuwurjofear.Inking.Erasing;

record PointPathEraserResult();

class PointPathEraserManager
{
    public void StartEraserPointPath(IReadOnlyList<SkiaStroke> staticStrokeList)
    {

    }

    private List<InkInfoForEraserPointPath> WorkList { get; set; } = null!;

    public void Move(Rect rect)
    {

    }

    public PointPathEraserResult Finish()
    {
        return new PointPathEraserResult();
    }

    class InkInfoForEraserPointPath
    {
        public InkInfoForEraserPointPath(SkiaStroke originSkiaStroke)
        {
            OriginSkiaStroke = originSkiaStroke;
        }

        public SkiaStroke OriginSkiaStroke { get; }

        /// <summary>
        /// 所有实际带的点
        /// </summary>
        /// 比 <see cref="StylusPoint"/> 结构体小，如此可以提升遍历性能
        public Point[] PointList { get; }
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