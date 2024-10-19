using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Utils;

public static class SkiaExtensions
{
    public static SKRect ToSKRect(this Rect rect)
    {
        return new SKRect((float) rect.Left, (float) rect.Top, (float) rect.Right, (float) rect.Bottom);
    }

    public static Rect ToRect(this SKRect rect)
    {
        return Rect.FromLeftTopRightBottom(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public static SKPoint ToSKPoint(this Point point)
    {
        return new SKPoint((float) point.X, (float) point.Y);
    }
}