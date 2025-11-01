using System.Collections.Generic;
using DotNetCampus.Numerics.Geometry;

namespace WpfInk;

public interface IStreamGeometryContext
{
    void BeginFigure(Point2D startPoint, bool isFilled, bool isClosed);
    void PolyBezierTo(IList<Point2D> points, bool isStroked, bool isSmoothJoin);
    void PolyLineTo(IList<Point2D> points, bool isStroked, bool isSmoothJoin);
    void ArcTo(Point2D point, Size2D size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked, bool isSmoothJoin);
}