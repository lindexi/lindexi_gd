using System.Windows.Media;
using WpfInk;
using IStreamGeometryContext = WpfInk.IStreamGeometryContext;

namespace WpfApp.Inking;

class InkingStreamGeometryContext : IStreamGeometryContext
{
    public InkingStreamGeometryContext(StreamGeometryContext streamGeometryContext)
    {
        StreamGeometryContext = streamGeometryContext;
    }

    private StreamGeometryContext StreamGeometryContext { get; }

    public void BeginFigure(InkPoint2D startPoint, bool isFilled, bool isClosed)
    {
        StreamGeometryContext.BeginFigure(startPoint.ToPoint(), isFilled, isClosed);
    }

    public void PolyBezierTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin)
    {
        StreamGeometryContext.PolyBezierTo(points.Select(t => t.ToPoint()).ToList(), isStroked, isSmoothJoin);
    }

    public void PolyLineTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin)
    {
        StreamGeometryContext.PolyLineTo(points.Select(t => t.ToPoint()).ToList(), isStroked, isSmoothJoin);
    }

    public void ArcTo(InkPoint2D point, InkSize2D size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        StreamGeometryContext.ArcTo(point.ToPoint(), size.ToSize(), rotationAngle, isLargeArc, sweepDirection ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, isStroked, isSmoothJoin);
    }
}