using Microsoft.Maui.Graphics;

namespace NarjejerechowainoBuwurjofear.Inking;

static class MauiRectExtension
{
    public static Rect Union(this Rect rect, Point point)
    {
        if (rect.IsEmpty)
        {
            return new Rect(point.X, point.Y, 0, 0);
        }

        return new Rect
        (
            Math.Min(rect.Left, point.X),
            Math.Min(rect.Top, point.Y),
            Math.Max(rect.Right, point.X),
            Math.Max(rect.Bottom, point.Y)
        );
    }

    public static Rect ToMauiRect(this Avalonia.Rect rect)
    {
        return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public static Avalonia.Rect ToAvaloniaRect(this Rect rect)
    {
        return new Avalonia.Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }
}