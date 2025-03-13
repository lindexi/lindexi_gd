using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Utils;

public static class SkiaExtensions
{
    public static SKRect ToSKRect(this TextRect rect)
    {
        return new SKRect((float) rect.Left, (float) rect.Top, (float) rect.Right, (float) rect.Bottom);
    }

    /// <summary>
    /// 转换为 TextRect 对象
    /// </summary>
    /// 这个方法不公开，不要污染其他模块。只有文本里面才可能使用到
    /// <param name="rect"></param>
    /// <returns></returns>
    internal static TextRect ToTextRect(this SKRect rect)
    {
        return TextRect.FromLeftTopRightBottom(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public static SKPoint ToSKPoint(this TextPoint point)
    {
        return new SKPoint((float) point.X, (float) point.Y);
    }
}