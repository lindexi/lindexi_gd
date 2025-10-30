using DotNetCampus.Numerics.Geometry;
using SkiaSharp;

namespace DotNetCampus.Inking.Utils;

static class AvaloniaRectExtension
{
    public static Rect2D ToRect2D(this SKRect rect)
    {
        return new Rect2D(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    public static Rect2D ToRect2D(this Avalonia.Rect rect)
    {
        return new Rect2D(rect.Left, rect.Top, rect.Width, rect.Height);
    }
}