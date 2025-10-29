using SkiaSharp;

namespace SkiaInkCore.Primitive;

static class RectExtension
{
    public static SKRectI LimitRect(SKRectI inputRect, SKRectI maxRect)
    {
        var left = inputRect.Left;
        var top = inputRect.Top;
        var right = inputRect.Right;
        var bottom = inputRect.Bottom;

        left = Math.Max(left, maxRect.Left);
        top = Math.Max(top, maxRect.Top);
        right = Math.Min(right, maxRect.Right);
        bottom = Math.Min(bottom, maxRect.Bottom);

        var width = right - left;
        var height = bottom - top;

        if (width <= 0 || height <= 0)
        {
            return SKRectI.Empty;
        }

        return SKRectI.Create(left, top, width, height);
    }

    public static SKRect LimitRect(SKRect inputRect, SKRect maxRect)
    {
        var left = inputRect.Left;
        var top = inputRect.Top;
        var right = inputRect.Right;
        var bottom = inputRect.Bottom;

        left = Math.Max(left, maxRect.Left);
        top = Math.Max(top, maxRect.Top);
        right = Math.Min(right, maxRect.Right);
        bottom = Math.Min(bottom, maxRect.Bottom);

        var width = right - left;
        var height = bottom - top;

        if (width <= 0 || height <= 0)
        {
            return SKRect.Empty;
        }

        return SKRect.Create(left, top, width, height);
    }

    public static Rect LimitRect(Rect inputRect, Rect maxRect)
    {
        var left = inputRect.Left;
        var top = inputRect.Top;
        var right = inputRect.Right;
        var bottom = inputRect.Bottom;

        left = Math.Max(left, maxRect.Left);
        top = Math.Max(top, maxRect.Top);
        right = Math.Min(right, maxRect.Right);
        bottom = Math.Min(bottom, maxRect.Bottom);

        var width = right - left;
        var height = bottom - top;

        if (width <= 0 || height <= 0)
        {
            return Rect.Zero;
        }

        return new Rect(left, top, width, height);
    }

    public static Rect Expand(SKRect rect, double addition)
    {
        return new Rect(rect.Left - addition, rect.Top - addition,
            rect.Width + addition * 2, rect.Height + addition * 2);
    }

    public static SKRect ExpandSKRect(SKRect rect, float addition)
    {
        return new SKRect(rect.Left - addition, rect.Top - addition,
            rect.Width + addition * 2, rect.Height + addition * 2);
    }
    
    public static Rect ToMauiRect(this SKRect rect)
    {
        return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    public static SKRect ToSkRect(this Rect rect)
    {
        return new SKRect((float) rect.Left, (float) rect.Top, (float) rect.Right, (float) rect.Bottom);
    }
}
