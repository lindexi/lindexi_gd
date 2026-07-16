using System.Windows;
using System.Windows.Media;

namespace CakawuhealehaneJairijelanefer;

internal static class IconDrawingFactory
{
    internal const double LogicalSize = 128;

    private static readonly Brush OutlineBrush = CreateBrush(0x4F, 0x7F, 0xAE);
    private static readonly Brush BaseTopBrush = CreateBrush(0xD8, 0xEB, 0xFA);
    private static readonly Brush BaseLeftBrush = CreateBrush(0xB7, 0xD5, 0xEE);
    private static readonly Brush BaseRightBrush = CreateBrush(0x9E, 0xC6, 0xE6);
    private static readonly Brush CubeTopBrush = CreateBrush(0xDB, 0xED, 0xFA);
    private static readonly Brush CubeLeftBrush = CreateBrush(0xB5, 0xD4, 0xEE);
    private static readonly Brush CubeRightBrush = CreateBrush(0x97, 0xC2, 0xE4);

    internal static DrawingGroup CreateDrawing()
    {
        Pen outlinePen = new(OutlineBrush, 2.5)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        outlinePen.Freeze();

        DrawingGroup drawing = new();
        using (DrawingContext drawingContext = drawing.Open())
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, LogicalSize, LogicalSize));

            DrawPolygon(drawingContext, BaseTopBrush, outlinePen,
                new Point(18, 61),
                new Point(64, 34),
                new Point(110, 61),
                new Point(64, 89));
            DrawPolygon(drawingContext, BaseLeftBrush, outlinePen,
                new Point(18, 61),
                new Point(64, 89),
                new Point(64, 116),
                new Point(18, 88));
            DrawPolygon(drawingContext, BaseRightBrush, outlinePen,
                new Point(64, 89),
                new Point(110, 61),
                new Point(110, 88),
                new Point(64, 116));

            DrawPolygon(drawingContext, CubeTopBrush, outlinePen,
                new Point(40, 34),
                new Point(64, 19),
                new Point(88, 34),
                new Point(64, 49));
            DrawPolygon(drawingContext, CubeLeftBrush, outlinePen,
                new Point(40, 34),
                new Point(64, 49),
                new Point(64, 77),
                new Point(40, 62));
            DrawPolygon(drawingContext, CubeRightBrush, outlinePen,
                new Point(64, 49),
                new Point(88, 34),
                new Point(88, 62),
                new Point(64, 77));
        }

        drawing.Freeze();
        return drawing;
    }

    private static void DrawPolygon(DrawingContext drawingContext, Brush fill, Pen outline, params Point[] points)
    {
        StreamGeometry geometry = new();
        using (StreamGeometryContext geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(points[0], isFilled: true, isClosed: true);
            geometryContext.PolyLineTo(points.AsSpan(1).ToArray(), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(fill, outline, geometry);
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        SolidColorBrush brush = new(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
