using System.Windows;
using System.Windows.Media;
using WpfInk;

namespace WpfApp.Inking;

class InkingStreamGeometryContext : IStreamGeometryContext
{
    public InkingStreamGeometryContext(StreamGeometryContext streamGeometryContext)
    {
        StreamGeometryContext = streamGeometryContext;
    }

    private StreamGeometryContext StreamGeometryContext { get; }
    private readonly List<Point> _cacheList = new List<Point>();

    public void BeginFigure(InkPoint2D startPoint, bool isFilled, bool isClosed)
    {
        StreamGeometryContext.BeginFigure(startPoint.ToPoint(), isFilled, isClosed);
    }

    public void PolyBezierTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin)
    {
        _cacheList.Clear();
        _cacheList.AddRange(points.Select(t => t.ToPoint()));
        StreamGeometryContext.PolyBezierTo(_cacheList, isStroked, isSmoothJoin);
    }

    public void PolyLineTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin)
    {
        _cacheList.Clear();
        _cacheList.AddRange(points.Select(t => t.ToPoint()));
        StreamGeometryContext.PolyLineTo(_cacheList, isStroked, isSmoothJoin);
    }

    public void ArcTo(InkPoint2D point, InkSize2D size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        StreamGeometryContext.ArcTo(point.ToPoint(), size.ToSize(), rotationAngle, isLargeArc, sweepDirection ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, isStroked, isSmoothJoin);
    }
}