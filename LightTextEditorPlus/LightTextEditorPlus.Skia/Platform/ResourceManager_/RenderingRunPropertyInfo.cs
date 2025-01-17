using System;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 渲染时的字符属性信息
/// </summary>
internal readonly record struct RenderingRunPropertyInfo(SKTypeface Typeface, SKFont Font, SKPaint Paint)
{
  
}

/// <summary>
/// 用于缓存的渲染时的字符属性信息
/// </summary>
/// <param name="Typeface"></param>
/// <param name="Font"></param>
/// <param name="Paint"></param>
internal readonly record struct CacheRenderingRunPropertyInfo(SKTypeface Typeface, SKFont? Font, SKPaint? Paint)
    : IDisposable
{
    public void Dispose()
    {
        Typeface.Dispose();
        Font?.Dispose();
        Paint?.Dispose();
    }
}
