using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NaN_Crash
{
    internal class Class1 : Control
    {
        protected override void OnRender(DrawingContext dc)
        {
            var rc = new Rect(0, 0, ActualWidth, ActualHeight);

            // bad rc
            rc = new Rect(0, double.NaN, 36, 144);
            Geometry.DrawRoundedRect(dc, rc, 18);
        }
    }

    internal static class Geometry
    {
        static void Curve(PathFigure pf, double x, double y, double cr, bool clockwise)
        {
            pf.Segments.Add(
              new ArcSegment(
                new Point(x, y),
                new Size(cr, cr),
                0,
                false,
                clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                true));
        }

        static void Line(PathFigure pf, double x, double y)
        {
            pf.Segments.Add(new LineSegment(new Point(x, y), true));
        }

        public static PathGeometry GetRoundedRect(Rect rect, double cr)
        {
            PathGeometry pg = new PathGeometry();
            PathFigure pf = new PathFigure();
            pf.StartPoint = new Point(rect.Left + cr, rect.Top);
            Line(pf, rect.Right - cr, rect.Top);
            Curve(pf, rect.Right, rect.Top + cr, cr, true);
            Line(pf, rect.Right, rect.Bottom - cr);
            Curve(pf, rect.Right - cr, rect.Bottom, cr, true);
            Line(pf, rect.Left + cr, rect.Bottom);
            Curve(pf, rect.Left, rect.Bottom - cr, cr, true);
            Line(pf, rect.Left, rect.Top + cr);
            Curve(pf, rect.Left + cr, rect.Top, cr, true);
            pf.IsClosed = true;
            pf.Freeze();
            pg.Figures.Add(pf);
            pg.Freeze();
            return pg;
        }

        public struct InnerGeometryInfo
        {
            public System.Windows.Media.Geometry Geo { get; set; }
            public double InnerBr { get; set; }
            public Rect InnerRect { get; set; }
        }

        public static InnerGeometryInfo GetInnerGeo(Rect rect, double br)
        {
            var th = new Thickness(1, 1, 1, 1);
            var innerRect = Adjust(rect, th);
            double innerBr = Math.Max(0, br - 1.0);

            return new InnerGeometryInfo()
            {
                Geo = GetRoundedRect(innerRect, innerBr),
                InnerBr = innerBr,
                InnerRect = innerRect
            };
        }

        public static void DrawRoundedRect(DrawingContext dc, Rect rect, double br)
        {
            var geo1 = GetRoundedRect(rect, br);
            var innerInfo = GetInnerGeo(rect, br);
            var innerBr = innerInfo.InnerBr;
            var innerRect = Adjust(rect, new Thickness(1.0));
            var geo = new CombinedGeometry(GeometryCombineMode.Exclude, geo1, innerInfo.Geo);
            dc.PushClip(geo);
            dc.Pop();
        }

        public static Rect SafeRect(double x, double y, double w, double h)
        {
            return new Rect(x, y, Math.Max(0, w), Math.Max(0, h));
        }

        public static Rect Adjust(Rect rc, Thickness? th)
        {
            if (th.HasValue)
            {
                return SafeRect(rc.Left + th.Value.Left, rc.Top + th.Value.Top, rc.Width - th.Value.Left - th.Value.Right, rc.Height - th.Value.Top - th.Value.Bottom);
            }
            return rc;
        }
    }

}
