using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Utils;

public static class SkiaExtensions
{
    public static SKRect ToSKRect(this TextRect rect)
    {
        return new SKRect((float) rect.Left, (float) rect.Top, (float) rect.Right, (float) rect.Bottom);
    }

    public static TextRect ToRect(this SKRect rect)
    {
        return TextRect.FromLeftTopRightBottom(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public static SKPoint ToSKPoint(this TextPoint point)
    {
        return new SKPoint((float) point.X, (float) point.Y);
    }
}