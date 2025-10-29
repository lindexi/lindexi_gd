using System.Collections.Generic;

namespace WpfInk;

public interface IStreamGeometryContext
{
    void BeginFigure(InkPoint2D startPoint, bool isFilled, bool isClosed);
    void PolyBezierTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin);
    void PolyLineTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin);
    void ArcTo(InkPoint2D point, InkSize2D size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked, bool isSmoothJoin);
}