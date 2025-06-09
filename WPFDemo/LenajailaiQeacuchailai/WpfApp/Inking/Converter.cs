extern alias WpfInk;
using WpfInk::WpfInk.PresentationCore.System.Windows;
using Size = System.Windows.Size;

namespace WpfApp.Inking;

static class Converter
{
    public static Point ToPoint(this System.Windows.Point point)
    {
        return new WpfInk::WpfInk.PresentationCore.System.Windows.Point(point.X, point.Y);
    }

    public static WpfInk::WpfInk.PresentationCore.System.Windows.Size ToSize(this Size size)
    {
        return new WpfInk::WpfInk.PresentationCore.System.Windows.Size(size.Width, size.Height);
    }

    public static System.Windows.Point ToPoint(this WpfInk::WpfInk.PresentationCore.System.Windows.Point point)
    {
        return new System.Windows.Point(point.X, point.Y);
    }

    public static Size ToSize(this WpfInk::WpfInk.PresentationCore.System.Windows.Size size)
    {
        return new Size(size.Width, size.Height);
    }
}