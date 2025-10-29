using InkBase;

namespace WpfInk;

static class StandardRgbColorExtension
{
    public static System.Windows.Media.Color ToWpfColor(this StandardRgbColor color)
    {
        return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}