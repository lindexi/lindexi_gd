using DotNetCampus.Numerics.Geometry;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WpfInk;

namespace DotNetCampus.Inking.StrokeRenderers.WpfForSkiaInkStrokeRenderers;

public class SkiaStreamGeometryContext : IStreamGeometryContext
{
    public SkiaStreamGeometryContext(SKPath path)
    {
        Path = path;
        path.FillType = SKPathFillType.Winding;
    }

    public SKPath Path { get; }

    public void BeginFigure(Point2D startPoint, bool isFilled, bool isClosed)
    {
        Path.MoveTo(startPoint.ToPoint());
    }

    public void PolyBezierTo(IList<Point2D> points, bool isStroked, bool isSmoothJoin)
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

    public void PolyLineTo(IList<Point2D> points, bool isStroked, bool isSmoothJoin)
    {
        foreach (var point in points)
        {
            Path.LineTo(point.ToPoint());
        }
    }

    public void ArcTo(Point2D point, Size2D size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        Path.ArcTo((float) size.Width, (float) size.Height, (float) rotationAngle, isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small, sweepDirection ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise, (float) point.X, (float) point.Y);
    }
}

file static class Converter
{
    public static SKPoint ToPoint(this Point2D point)
    {
        return new SKPoint((float) point.X, (float) point.Y);
    }
}