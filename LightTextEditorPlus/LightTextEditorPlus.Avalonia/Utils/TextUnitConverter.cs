using Avalonia;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Utils;

static class TextUnitConverter
{
    public static TextPoint ToTextPoint(this Point point)
        => new(point.X, point.Y);

    public static Point ToPoint(this TextPoint textPoint)
        => new(textPoint.X, textPoint.Y);

    public static TextRect ToTextRect(this Rect rect)
        => TextRect.FromLeftTopRightBottom(rect.Left, rect.Top, rect.Right, rect.Bottom);

    public static Rect ToAvaloniaRect(this TextRect textRect)
        => new Rect(textRect.X, textRect.Y, textRect.Width, textRect.Height);
}