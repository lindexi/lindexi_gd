using System.Collections.Generic;
using System.Linq;
using WpfInk.PresentationCore.System.Windows;

namespace WpfInk;

internal class InternalStreamGeometryContext : IInternalStreamGeometryContext
{
    public InternalStreamGeometryContext(IStreamGeometryContext context)
    {
        _context = context;
    }

    private readonly IStreamGeometryContext _context;
    private List<InkPoint2D> _cacheList = new List<InkPoint2D>();

    public void BeginFigure(InkPoint2D startPoint, bool isFilled, bool isClosed)
    {
        _context.BeginFigure(startPoint.ToPoint(), isFilled, isClosed);
    }

    public void PolyBezierTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin)
    {
        _cacheList.Clear();
        _cacheList.AddRange(points.Select(t => Converter.ToPoint(t)));
        _context.PolyBezierTo(_cacheList, isStroked, isSmoothJoin);
    }

    public void PolyLineTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin)
    {
        _cacheList.Clear();
        _cacheList.AddRange(points.Select(t => t.ToPoint()));
        _context.PolyLineTo(_cacheList, isStroked, isSmoothJoin);
    }

    public void ArcTo(InkPoint2D point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        _context.ArcTo(point.ToPoint(), size.ToSize(), rotationAngle, isLargeArc, sweepDirection, isStroked, isSmoothJoin);
    }
}