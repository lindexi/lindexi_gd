using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Point = Microsoft.Maui.Graphics.Point;
using Rect = Microsoft.Maui.Graphics.Rect;

using UnoInk.Inking.InkCore;

namespace NarjejerechowainoBuwurjofear.Inking;

class AvaSkiaInkCanvasEraserMode
{
    public AvaSkiaInkCanvasEraserMode(AvaSkiaInkCanvas inkCanvas)
    {
        InkCanvas = inkCanvas;
    }

    public AvaSkiaInkCanvas InkCanvas { get; }
    public bool IsErasing { get; private set; }
    private int MainEraserInputId { set; get; }

 

    public void StartEraser()
    {
        var staticStrokeList = InkCanvas.StaticStrokeList;
        
    }

    public void EraserDown(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (!IsErasing)
        {
            MainEraserInputId = args.Id;

            IsErasing = true;
        }
        else
        {
            // 忽略其他的输入点
        }
    }

    public void EraserMove(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            // 擦除
        }
    }

    public void EraserUp(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            IsErasing = false;
        }
    }

    public void Render(DrawingContext context)
    {
        context.Custom(new EraserModeCustomDrawOperation(this));
    }

    class EraserModeCustomDrawOperation : ICustomDrawOperation
    {
        public EraserModeCustomDrawOperation(AvaSkiaInkCanvasEraserMode eraserMode)
        {

        }

        public void Dispose()
        {
            
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Avalonia.Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
        }

        public Avalonia.Rect Bounds { get; }
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

static class MauiRectExtension
{
    public static Rect Union(this Rect rect, Point point)
    {
        if (rect.IsEmpty)
        {
            return new Rect(point.X, point.Y, 0, 0);
        }

        return new Rect
        (
            Math.Min(rect.Left, point.X),
            Math.Min(rect.Top, point.Y),
            Math.Max(rect.Right, point.X),
            Math.Max(rect.Bottom, point.Y)
        );
    }

    public static Rect ToMauiRect(this Avalonia.Rect rect)
    {
        return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public static Avalonia.Rect ToAvaloniaRect(this Rect rect)
    {
        return new Avalonia.Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }
}

static class PointExtension
{
    public static Point ToPoint(this Avalonia.Point point)
    {
        return new Point(point.X, point.Y);
    }

    public static Avalonia.Point ToAvaloniaPoint(this Point point)
    {
        return new Avalonia.Point(point.X, point.Y);
    }
}