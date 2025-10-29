using Point = WpfInk.InkPoint2D;
using Size = WpfInk.InkSize2D;

namespace WpfApp.Inking;

static class Converter
{
    public static Point ToPoint(this System.Windows.Point point)
    {
        return new Point(point.X, point.Y);
    }

    public static Size ToSize(this System.Windows.Size size)
    {
        return new Size(size.Width, size.Height);
    }

    public static System.Windows.Point ToPoint(this Point point)
    {
        return new System.Windows.Point(point.X, point.Y);
    }

    public static System.Windows.Size ToSize(this Size size)
    {
        return new System.Windows.Size(size.Width, size.Height);
    }
}