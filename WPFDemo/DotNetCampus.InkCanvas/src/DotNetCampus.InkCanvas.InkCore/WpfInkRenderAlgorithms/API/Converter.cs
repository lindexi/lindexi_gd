using DotNetCampus.Inking.Primitive;
using DotNetCampus.Numerics.Geometry;
using WpfInk.PresentationCore.System.Windows;
using WpfInk.PresentationCore.System.Windows.Input.Stylus;

namespace WpfInk;

static class Converter
{
    public static Point2D ToPoint(this Point point) => new Point2D(point.X, point.Y);
    public static Size2D ToSize(this Size size) => new Size2D(size.Width, size.Height);

    public static StylusPoint ToStylusPoint(this InkStylusPoint stylusPoint)
    {
        return new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.Pressure);
    }
}