using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SkiaSharp;

using WpfInk.API;
using WpfInk.PresentationCore.System.Windows;

namespace SkiaInk;

internal class SkiaStreamGeometryContext : IStreamGeometryContext
{
    public SkiaStreamGeometryContext(SKPath path)
    {
        Path = path;
    }

    public SKPath Path { get; }

    public void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
    {
        throw new NotImplementedException();
    }

    public void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
    {
    }

    public void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
    {
        throw new NotImplementedException();
    }

    public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        
    }
}
