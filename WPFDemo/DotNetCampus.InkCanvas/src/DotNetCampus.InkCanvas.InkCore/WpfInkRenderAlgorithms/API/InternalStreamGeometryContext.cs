using System.Collections.Generic;
using System.Linq;
using DotNetCampus.Numerics.Geometry;
using WpfInk.PresentationCore.System.Windows;

namespace WpfInk;

internal class InternalStreamGeometryContext : IInternalStreamGeometryContext
{
    public InternalStreamGeometryContext(IStreamGeometryContext context)
    {
        _context = context;
    }

    private readonly IStreamGeometryContext _context;
    private readonly List<Point2D> _cacheList = new List<Point2D>();

    public void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
    {
        _context.BeginFigure(startPoint.ToPoint(), isFilled, isClosed);
    }

    public void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
    {
        _cacheList.Clear();
        _cacheList.AddRange(points.Select(t => t.ToPoint()));
        _context.PolyBezierTo(_cacheList, isStroked, isSmoothJoin);
    }

    public void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
    {
        _cacheList.Clear();
        _cacheList.AddRange(points.Select(t => t.ToPoint()));
        _context.PolyLineTo(_cacheList, isStroked, isSmoothJoin);
    }

    public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        _context.ArcTo(point.ToPoint(), size.ToSize(), rotationAngle, isLargeArc, sweepDirection, isStroked, isSmoothJoin);
    }
}