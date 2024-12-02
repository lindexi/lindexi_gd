using SkiaSharp;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 渲染时的字符属性信息
/// </summary>
internal readonly record struct RenderingRunPropertyInfo(SKTypeface Typeface, SKFont Font, SKPaint Paint) : IDisposable
{
    public void Dispose()
    {
        Typeface.Dispose();
        Font.Dispose();
        Paint.Dispose();
    }
}