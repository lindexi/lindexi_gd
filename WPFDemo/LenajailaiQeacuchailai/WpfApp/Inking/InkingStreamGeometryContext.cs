extern alias WpfInk;
using System.Windows.Media;
using WpfInk::WpfInk.API;

namespace WpfApp.Inking;

class InkingStreamGeometryContext : IStreamGeometryContext
{
    public InkingStreamGeometryContext(StreamGeometryContext streamGeometryContext)
    {
        StreamGeometryContext = streamGeometryContext;
    }

    private StreamGeometryContext StreamGeometryContext { get; }

    public void BeginFigure(WpfInk::WpfInk.PresentationCore.System.Windows.Point startPoint, bool isFilled, bool isClosed)
    {
        StreamGeometryContext.BeginFigure(startPoint.ToPoint(), isFilled, isClosed);
    }

    public void PolyBezierTo(IList<WpfInk::WpfInk.PresentationCore.System.Windows.Point> points, bool isStroked, bool isSmoothJoin)
    {
        StreamGeometryContext.PolyBezierTo(points.Select(t => t.ToPoint()).ToList(), isStroked, isSmoothJoin);
    }

    public void PolyLineTo(IList<WpfInk::WpfInk.PresentationCore.System.Windows.Point> points, bool isStroked, bool isSmoothJoin)
    {
        StreamGeometryContext.PolyLineTo(points.Select(t => t.ToPoint()).ToList(), isStroked, isSmoothJoin);
    }

    public void ArcTo(WpfInk::WpfInk.PresentationCore.System.Windows.Point point, WpfInk::WpfInk.PresentationCore.System.Windows.Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        StreamGeometryContext.ArcTo(point.ToPoint(), size.ToSize(), rotationAngle, isLargeArc, sweepDirection ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, isStroked, isSmoothJoin);
    }
}