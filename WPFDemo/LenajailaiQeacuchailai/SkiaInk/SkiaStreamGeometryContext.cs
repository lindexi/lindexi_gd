using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SkiaSharp;

using WpfInk.API;
using WpfInk.PresentationCore.System.Windows;

namespace SkiaInk;

public class SkiaStreamGeometryContext : IStreamGeometryContext
{
    public SkiaStreamGeometryContext(SKPath path)
    {
        Path = path;
        path.FillType = SKPathFillType.Winding;
    }

    public SKPath Path { get; }

    public void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
    {
        Path.MoveTo(startPoint.ToPoint());
    }

    public void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
    {
        // 传入的 points 必定是 3 的倍数

        for (var i = 0; i < points.Count; i += 3)
        {
            var a = points[i];
            var b = points[i + 1];
            var c = points[i + 2];

            Path.CubicTo(a.ToPoint(), b.ToPoint(), c.ToPoint());
        }
    }

    public void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
    {
        foreach (var point in points)
        {
            Path.LineTo(point.ToPoint());
        }
    }

    public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        Path.ArcTo((float) size.Width, (float) size.Height, (float) rotationAngle, isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small, sweepDirection ? SKPathDirection.CounterClockwise : SKPathDirection.Clockwise, (float) point.X, (float) point.Y);
    }
}

static class Converter
{
    public static SKPoint ToPoint(this Point point)
    {
        return new SKPoint((float) point.X, (float) point.Y);
    }
}