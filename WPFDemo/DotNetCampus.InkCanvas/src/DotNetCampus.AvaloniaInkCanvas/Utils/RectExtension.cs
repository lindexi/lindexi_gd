using Avalonia;

namespace DotNetCampus.Inking.Utils;

static class RectExtension
{
    public static Rect ExpandLength(this Rect rect, double value)
    {
        return new Rect(rect.X - value / 2, rect.Y - value / 2, rect.Width + value, rect.Height + value);
    }
}