using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using MS.Internal.Ink;

using WpfInk.PresentationCore.System.Windows;
using WpfInk.PresentationCore.System.Windows.Ink;
using WpfInk.PresentationCore.System.Windows.Input.Stylus;

namespace WpfInk;

internal interface IInternalStreamGeometryContext
{
    void BeginFigure(Point startPoint, bool isFilled, bool isClosed);
    void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin);
    void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin);
    void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked, bool isSmoothJoin);
}

internal class InternalStreamGeometryContext : IInternalStreamGeometryContext
{
    public InternalStreamGeometryContext(IStreamGeometryContext context)
    {
        _context = context;
    }

    private readonly IStreamGeometryContext _context;
    private List<InkPoint2D> _cacheList = new List<InkPoint2D>();

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

static class Converter
{
    public static InkPoint2D ToPoint(this Point point) => new InkPoint2D(point.X, point.Y);
    public static InkSize2D ToSize(this Size size) => new InkSize2D(size.Width, size.Height);

    public static StylusPoint ToStylusPoint(this InkStylusPoint2D stylusPoint)
    {
        return new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.Pressure);
    }
}

public interface IStreamGeometryContext
{
    void BeginFigure(InkPoint2D startPoint, bool isFilled, bool isClosed);
    void PolyBezierTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin);
    void PolyLineTo(IList<InkPoint2D> points, bool isStroked, bool isSmoothJoin);
    void ArcTo(InkPoint2D point, InkSize2D size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked, bool isSmoothJoin);
}

public readonly record struct InkStylusPoint2D(double X, double Y, float Pressure = StylusPoint.DefaultPressure)
{
    public InkStylusPoint2D(InkPoint2D point) : this(point.X, point.Y)
    {
    }
}

public readonly record struct InkPoint2D(double X, double Y);
public readonly record struct InkSize2D(double Width, double Height);

public static class InkStrokeRenderer
{
    public static void Render(IStreamGeometryContext streamGeometryContext, in StrokeRendererInfo info)
    {
        var drawingAttributes = new DrawingAttributes()
        {
            Width = info.Width,
            Height = info.Height,
        };

        var stylusPointCollection = new StylusPointCollection();

        foreach (InkStylusPoint2D inkStylusPoint2D in info.StylusPointCollection)
        {
            stylusPointCollection.Add(inkStylusPoint2D.ToStylusPoint());
        }

        var stroke = new Stroke(stylusPointCollection, drawingAttributes);
        StrokeNodeIterator strokeNodeIterator = StrokeNodeIterator.GetIterator(stroke, drawingAttributes);
        var internalStreamGeometryContext = new InternalStreamGeometryContext(streamGeometryContext);
        StrokeRenderer.CalcGeometryAndBounds(strokeNodeIterator, drawingAttributes, calculateBounds: false, internalStreamGeometryContext, out _);
    }
}

public readonly record struct StrokeRendererInfo
{
    public required IReadOnlyList<InkStylusPoint2D> StylusPointCollection { get; init; }

    public required double Width { get; init; }
    public required double Height { get; init; }

    public bool FitToCurve { get; init; }
}