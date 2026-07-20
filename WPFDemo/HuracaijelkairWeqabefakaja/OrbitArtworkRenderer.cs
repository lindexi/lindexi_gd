using System.Windows;
using System.Windows.Media;

namespace HuracaijelkairWeqabefakaja;

internal static class OrbitArtworkRenderer
{
    internal const int DesignSize = 512;

    private static readonly Rect DesignBounds = new(0, 0, DesignSize, DesignSize);
    private static readonly Point ArtworkCenter = new(256, 256);

    internal static DrawingGroup CreateDrawing()
    {
        var drawingGroup = new DrawingGroup
        {
            ClipGeometry = new RectangleGeometry(DesignBounds)
        };

        using (DrawingContext drawingContext = drawingGroup.Open())
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, DesignBounds);
            DrawOuterOrbit(drawingContext);
            DrawMainRing(drawingContext);
            DrawInnerOrbits(drawingContext);
        }

        drawingGroup.Freeze();
        return drawingGroup;
    }

    private static void DrawOuterOrbit(DrawingContext drawingContext)
    {
        var orbitPen = CreatePen(CreateArtworkGradient(), 4.5);
        drawingContext.DrawEllipse(null, orbitPen, ArtworkCenter, 194, 191);

        var echoPen = CreatePen(CreateArtworkGradient(0.35), 1.25);
        DrawBezier(
            drawingContext,
            echoPen,
            new Point(92, 339),
            new BezierSegment(new Point(126, 431), new Point(232, 471), new Point(330, 437)),
            new BezierSegment(new Point(382, 419), new Point(426, 377), new Point(447, 324)));

        OuterNode[] nodes =
        [
            new(-95, 18, NodeShape.Circle),
            new(-76, 13, NodeShape.Diamond),
            new(-59, 17, NodeShape.Circle),
            new(-40, 18, NodeShape.Circle),
            new(-22, 19, NodeShape.Circle),
            new(-7, 12, NodeShape.Diamond),
            new(13, 20, NodeShape.Circle),
            new(30, 13, NodeShape.Diamond),
            new(48, 18, NodeShape.Circle),
            new(65, 16, NodeShape.Circle),
            new(82, 14, NodeShape.Diamond),
            new(91, 20, NodeShape.Circle),
            new(110, 13, NodeShape.Diamond),
            new(127, 13, NodeShape.Diamond),
            new(145, 16, NodeShape.Circle),
            new(164, 19, NodeShape.Circle),
            new(184, 14, NodeShape.Diamond),
            new(203, 20, NodeShape.Circle),
            new(224, 17, NodeShape.Circle),
            new(244, 12, NodeShape.Diamond),
            new(260, 14, NodeShape.Circle),
            new(278, 11, NodeShape.Diamond),
            new(296, 15, NodeShape.Circle),
            new(315, 12, NodeShape.Diamond),
            new(337, 16, NodeShape.Circle)
        ];

        foreach (OuterNode node in nodes)
        {
            Point center = PointOnEllipse(ArtworkCenter, 194, 191, node.Angle);
            Color color = GetOrbitColor(node.Angle);
            Brush fill = CreateSolidBrush(color);

            if (node.Shape == NodeShape.Circle)
            {
                drawingContext.DrawEllipse(fill, CreatePen(Brushes.White, 2.5), center, node.Size, node.Size);
            }
            else
            {
                DrawDiamond(drawingContext, center, node.Size * 1.55, fill);
            }
        }
    }

    private static void DrawMainRing(DrawingContext drawingContext)
    {
        Geometry ringGeometry = CreateRingGeometry(ArtworkCenter, 146, 91);
        Brush artworkGradient = CreateArtworkGradient();
        drawingContext.DrawGeometry(artworkGradient, null, ringGeometry);

        drawingContext.DrawEllipse(null, CreatePen(CreateArtworkGradient(0.72), 2), ArtworkCenter, 146, 146);
        drawingContext.DrawEllipse(null, CreatePen(CreateArtworkGradient(0.9), 5), ArtworkCenter, 92, 92);

        drawingContext.PushClip(ringGeometry);

        var broadHighlightPen = CreatePen(CreateSolidBrush(Color.FromArgb(205, 255, 255, 255)), 5);
        DrawBezier(
            drawingContext,
            broadHighlightPen,
            new Point(130, 303),
            new BezierSegment(new Point(143, 190), new Point(247, 129), new Point(355, 174)),
            new BezierSegment(new Point(395, 191), new Point(414, 224), new Point(420, 263)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateSolidBrush(Color.FromArgb(185, 255, 255, 255)), 2.5),
            new Point(119, 267),
            new BezierSegment(new Point(159, 143), new Point(320, 112), new Point(408, 228)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateSolidBrush(Color.FromArgb(170, 255, 255, 255)), 2),
            new Point(151, 373),
            new BezierSegment(new Point(238, 413), new Point(369, 370), new Point(407, 276)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateSolidBrush(Color.FromArgb(145, 255, 255, 255)), 1.25),
            new Point(118, 321),
            new BezierSegment(new Point(205, 347), new Point(327, 336), new Point(403, 231)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateSolidBrush(Color.FromArgb(115, 255, 255, 255)), 1),
            new Point(181, 132),
            new BezierSegment(new Point(124, 220), new Point(139, 347), new Point(241, 401)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateSolidBrush(Color.FromArgb(95, 255, 255, 255)), 1),
            new Point(325, 127),
            new BezierSegment(new Point(425, 205), new Point(425, 319), new Point(337, 391)));

        DrawRingParticles(drawingContext);
        drawingContext.Pop();

        DrawMainRingNodes(drawingContext);
    }

    private static void DrawRingParticles(DrawingContext drawingContext)
    {
        Particle[] particles =
        [
            new(188, 153, 4.5, 255),
            new(159, 186, 5.5, 255),
            new(142, 222, 2, 230),
            new(126, 270, 2.5, 220),
            new(143, 318, 3.5, 255),
            new(171, 357, 6.5, 255),
            new(218, 386, 2, 225),
            new(254, 397, 2.5, 225),
            new(300, 385, 6, 255),
            new(348, 363, 2.5, 255),
            new(389, 317, 5.5, 255),
            new(406, 274, 2, 225),
            new(393, 218, 6, 255),
            new(354, 159, 2.5, 210),
            new(293, 126, 4, 240),
            new(231, 125, 2, 220),
            new(204, 204, 2, 180),
            new(317, 192, 2, 180),
            new(360, 251, 3.5, 240),
            new(328, 334, 2, 220),
            new(210, 347, 2, 220)
        ];

        foreach (Particle particle in particles)
        {
            Brush fill = CreateSolidBrush(Color.FromArgb(particle.Alpha, 255, 255, 255));
            drawingContext.DrawEllipse(fill, null, new Point(particle.X, particle.Y), particle.Radius, particle.Radius);
        }
    }

    private static void DrawMainRingNodes(DrawingContext drawingContext)
    {
        RingNode[] nodes =
        [
            new(-100, 10),
            new(-68, 16),
            new(-39, 10),
            new(-8, 16),
            new(31, 11),
            new(62, 17),
            new(93, 11),
            new(127, 15),
            new(160, 10),
            new(191, 15),
            new(224, 17),
            new(258, 11),
            new(292, 14),
            new(326, 17)
        ];

        foreach (RingNode node in nodes)
        {
            Point center = PointOnEllipse(ArtworkCenter, 146, 146, node.Angle);
            Brush fill = CreateSolidBrush(GetOrbitColor(node.Angle));
            drawingContext.DrawEllipse(fill, CreatePen(Brushes.White, 3), center, node.Radius, node.Radius);
        }

        DrawDiamond(drawingContext, new Point(367, 304), 15, Brushes.White);
        DrawDiamond(drawingContext, new Point(147, 211), 14, Brushes.White);
    }

    private static void DrawInnerOrbits(DrawingContext drawingContext)
    {
        var innerPen = CreatePen(CreateArtworkGradient(), 4.5);
        DrawBezier(
            drawingContext,
            innerPen,
            new Point(171, 263),
            new BezierSegment(new Point(178, 185), new Point(251, 158), new Point(320, 182)),
            new BezierSegment(new Point(391, 207), new Point(383, 306), new Point(323, 349)),
            new BezierSegment(new Point(252, 397), new Point(171, 343), new Point(171, 263)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateArtworkGradient(0.88), 2.5),
            new Point(183, 305),
            new BezierSegment(new Point(206, 380), new Point(320, 391), new Point(363, 313)),
            new BezierSegment(new Point(390, 263), new Point(357, 191), new Point(292, 177)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateArtworkGradient(0.65), 2),
            new Point(183, 225),
            new BezierSegment(new Point(229, 158), new Point(344, 157), new Point(376, 241)));

        DrawBezier(
            drawingContext,
            CreatePen(CreateSolidBrush(Color.FromArgb(125, 115, 50, 145)), 1.25),
            new Point(178, 286),
            new BezierSegment(new Point(236, 313), new Point(326, 307), new Point(367, 254)));

        DrawInnerNode(drawingContext, new Point(257, 173), 14, -78, true);
        DrawInnerNode(drawingContext, new Point(332, 194), 12, -35, false);
        DrawInnerNode(drawingContext, new Point(371, 279), 14, 8, false);
        DrawInnerNode(drawingContext, new Point(332, 348), 9, 43, false);
        DrawInnerNode(drawingContext, new Point(257, 369), 14, 88, true);
        DrawInnerNode(drawingContext, new Point(178, 341), 15, 135, false);
        DrawInnerNode(drawingContext, new Point(170, 272), 13, 179, false);
        DrawInnerNode(drawingContext, new Point(198, 200), 8, 226, false);

        drawingContext.DrawEllipse(Brushes.White, null, new Point(221, 146), 6, 6);
        drawingContext.DrawEllipse(Brushes.White, null, new Point(353, 188), 5, 5);
        drawingContext.DrawEllipse(Brushes.White, null, new Point(382, 238), 5, 5);
        drawingContext.DrawEllipse(Brushes.White, null, new Point(287, 394), 5, 5);
    }

    private static void DrawInnerNode(
        DrawingContext drawingContext,
        Point center,
        double radius,
        double angle,
        bool hasGlow)
    {
        if (hasGlow)
        {
            drawingContext.DrawEllipse(CreateGlowBrush(), null, center, radius * 1.65, radius * 1.65);
        }

        Brush fill = CreateSolidBrush(GetOrbitColor(angle));
        drawingContext.DrawEllipse(fill, CreatePen(Brushes.White, 2.5), center, radius, radius);

        if (hasGlow)
        {
            drawingContext.DrawEllipse(Brushes.White, null, center, 3.5, 3.5);
        }
    }

    private static Geometry CreateRingGeometry(Point center, double outerRadius, double innerRadius)
    {
        var outerGeometry = new EllipseGeometry(center, outerRadius, outerRadius);
        var innerGeometry = new EllipseGeometry(center, innerRadius, innerRadius);
        var ringGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, outerGeometry, innerGeometry);
        ringGeometry.Freeze();
        return ringGeometry;
    }

    private static void DrawDiamond(DrawingContext drawingContext, Point center, double size, Brush fill)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(new Point(center.X, center.Y - size), true, true);
            geometryContext.LineTo(new Point(center.X + size, center.Y), true, false);
            geometryContext.LineTo(new Point(center.X, center.Y + size), true, false);
            geometryContext.LineTo(new Point(center.X - size, center.Y), true, false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(fill, null, geometry);
    }

    private static void DrawBezier(
        DrawingContext drawingContext,
        Pen pen,
        Point start,
        params BezierSegment[] segments)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(start, false, false);
            foreach (BezierSegment segment in segments)
            {
                geometryContext.BezierTo(
                    segment.Control1,
                    segment.Control2,
                    segment.End,
                    true,
                    false);
            }
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, pen, geometry);
    }

    private static Point PointOnEllipse(Point center, double radiusX, double radiusY, double angle)
    {
        double radians = angle * Math.PI / 180;
        return new Point(
            center.X + radiusX * Math.Cos(radians),
            center.Y + radiusY * Math.Sin(radians));
    }

    private static Brush CreateArtworkGradient(double opacity = 1)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            Opacity = opacity,
            GradientStops =
            [
                new GradientStop(Color.FromRgb(255, 169, 86), 0),
                new GradientStop(Color.FromRgb(255, 112, 103), 0.28),
                new GradientStop(Color.FromRgb(231, 83, 132), 0.52),
                new GradientStop(Color.FromRgb(156, 62, 132), 0.75),
                new GradientStop(Color.FromRgb(101, 47, 143), 1)
            ]
        };

        brush.Freeze();
        return brush;
    }

    private static Brush CreateGlowBrush()
    {
        var brush = new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            GradientOrigin = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            GradientStops =
            [
                new GradientStop(Colors.White, 0),
                new GradientStop(Color.FromRgb(98, 232, 255), 0.18),
                new GradientStop(Color.FromRgb(174, 70, 220), 0.48),
                new GradientStop(Color.FromArgb(0, 174, 70, 220), 1)
            ]
        };

        brush.Freeze();
        return brush;
    }

    private static Brush CreateSolidBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen CreatePen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        pen.Freeze();
        return pen;
    }

    private static Color GetOrbitColor(double angle)
    {
        double normalizedAngle = ((angle % 360) + 360) % 360;
        ColorStop[] stops =
        [
            new(0, Color.FromRgb(117, 50, 145)),
            new(60, Color.FromRgb(96, 45, 139)),
            new(100, Color.FromRgb(126, 53, 141)),
            new(150, Color.FromRgb(220, 86, 121)),
            new(200, Color.FromRgb(246, 119, 101)),
            new(240, Color.FromRgb(255, 167, 86)),
            new(285, Color.FromRgb(255, 130, 99)),
            new(330, Color.FromRgb(213, 81, 129)),
            new(360, Color.FromRgb(117, 50, 145))
        ];

        for (int index = 0; index < stops.Length - 1; index++)
        {
            ColorStop start = stops[index];
            ColorStop end = stops[index + 1];
            if (normalizedAngle < start.Angle || normalizedAngle > end.Angle)
            {
                continue;
            }

            double progress = (normalizedAngle - start.Angle) / (end.Angle - start.Angle);
            return Interpolate(start.Color, end.Color, progress);
        }

        return stops[^1].Color;
    }

    private static Color Interpolate(Color start, Color end, double progress)
    {
        byte red = (byte)Math.Round(start.R + (end.R - start.R) * progress);
        byte green = (byte)Math.Round(start.G + (end.G - start.G) * progress);
        byte blue = (byte)Math.Round(start.B + (end.B - start.B) * progress);
        return Color.FromRgb(red, green, blue);
    }

    private readonly record struct BezierSegment(Point Control1, Point Control2, Point End);

    private readonly record struct OuterNode(double Angle, double Size, NodeShape Shape);

    private readonly record struct RingNode(double Angle, double Radius);

    private readonly record struct Particle(double X, double Y, double Radius, byte Alpha);

    private readonly record struct ColorStop(double Angle, Color Color);

    private enum NodeShape
    {
        Circle,
        Diamond
    }
}
