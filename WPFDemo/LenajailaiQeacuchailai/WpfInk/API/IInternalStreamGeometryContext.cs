using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfInk.PresentationCore.System.Windows;

namespace WpfInk;

internal interface IInternalStreamGeometryContext
{
    void BeginFigure(InkPoint2D startPoint, bool isFilled, bool isClosed);
    void PolyBezierTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin);
    void PolyLineTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin);
    void ArcTo(InkPoint2D point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked, bool isSmoothJoin);
}