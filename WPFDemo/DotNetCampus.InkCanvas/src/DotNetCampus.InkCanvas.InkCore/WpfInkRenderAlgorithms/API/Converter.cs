using WpfInk.PresentationCore.System.Windows;
using WpfInk.PresentationCore.System.Windows.Input.Stylus;

namespace WpfInk;

static class Converter
{
    public static InkPoint2D ToPoint(this Point point) => new InkPoint2D(point.X, point.Y);
    public static InkSize2D ToSize(this Size size) => new InkSize2D(size.Width, size.Height);

    public static StylusPoint ToStylusPoint(this InkStylusPoint2D stylusPoint)
    {
        return new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.Pressure);
    }
}