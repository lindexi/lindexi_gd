using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using HarfBuzzSharp;
using LightTextEditorPlus.Core.Document;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformResourceManager
{
    public SkiaPlatformResourceManager(SkiaTextEditor textEditor)
    {
        _skiaTextEditor = textEditor;
        textEditor.InternalRenderCompleted += TextEditor_InternalRenderCompleted;
    }
    private readonly SkiaTextEditor _skiaTextEditor;
    public SkiaTextEditorPlatformProvider PlatformProvider => _skiaTextEditor.SkiaTextEditorPlatformProvider;

    private void TextEditor_InternalRenderCompleted(object? sender, EventArgs e)
    {
        // 每次渲染完成，都可以清理缓存。只有渲染布局过程才会需要用到 Skia 资源
        foreach (KeyValuePair<SkiaTextRunProperty, RenderingRunPropertyInfo> renderingFontInfo in _cache)
        {
            RenderingRunPropertyInfo runPropertyInfo = renderingFontInfo.Value;
            runPropertyInfo.Dispose();
        }
        _cache.Clear();
        _cache.TrimExcess();
    }

    public bool CanFontSupportChar(SkiaTextRunProperty runProperty, ICharObject charObject)
    {
        // todo 测试 SKTypeface.FromFamilyName 性能
        using SKTypeface skTypeface = GetTypeface(runProperty);
        string text = charObject.ToText();
        return skTypeface.ContainsGlyphs(text);
    }

    public bool TryFallbackRunProperty(SkiaTextRunProperty runProperty, ICharObject charObject, [NotNullWhen(true)] out SkiaTextRunProperty? newRunProperty)
    {
        if (PlatformProvider.GetFontNameToSKTypefaceManager() is { } manager)
        {
            return manager.TryFallbackRunProperty(runProperty, charObject, out newRunProperty);
        }

        newRunProperty = null;
        return false;
    }

    private SKTypeface GetTypeface(SkiaTextRunProperty runProperty)
    {
        if (PlatformProvider.GetFontNameToSKTypefaceManager() is { } manager)
        {
            return manager.Resolve(runProperty);
        }
        else
        {
            // 这是默认实现
            var typeface = SKTypeface.FromFamilyName(runProperty.FontName.UserFontName, runProperty.FontWeight, runProperty.Stretch, runProperty.FontStyle);
            // 不处理未找到字体的情况。由上层保证

            return typeface;
        }

        //if (skTypeface?.ContainsGlyphs([unicodeChar]) is true)
        //{
        //    return new RenderingFontInfo(skTypeface);
        //}
    }

    public RenderingRunPropertyInfo GetRenderingRunPropertyInfo(SkiaTextRunProperty runProperty, char unicodeChar)
    {
        // todo 处理对齐情况
        if (_cache.TryGetValue(runProperty, out var cache))
        {
            // 不需要在这里处理找不到字体的情况
            return cache;
        }

        SKTypeface skTypeface = GetTypeface(runProperty);
        SKFont renderSkFont = new SKFont(skTypeface, (float) runProperty.FontSize);
        // From Avalonia
        // Ideally the requested edging should be passed to the glyph run.
        // Currently the edging is computed dynamically inside the drawing context, so we can't know it in advance.
        // But the bounds depends on the edging: for now, always use SubpixelAntialias so we have consistent values.
        // The resulting bounds may be shifted by 1px on some fonts:
        // "F" text with Inter size 14 has a 0px left bound with SubpixelAntialias but 1px with Antialias.

        var edging = SKFontEdging.SubpixelAntialias;

        renderSkFont.Hinting = SKFontHinting.Full;
        renderSkFont.Edging = edging;
        renderSkFont.Subpixel = edging != SKFontEdging.Alias;

        SKPaint skPaint = new SKPaint(renderSkFont);
        // skPaint 已经用上 SKFont 的字号属性，不需要再设置 TextSize 属性
        //skPaint.TextSize = runProperty.FontSize;
        skPaint.Color = runProperty.Foreground;

        if (runProperty.Opacity < 1)
        {
            // 处理透明度
            skPaint.Color = skPaint.Color.WithAlpha((byte) (skPaint.Color.Alpha * runProperty.Opacity));
        }

        skPaint.IsAntialias = true;

        var info = new RenderingRunPropertyInfo(skTypeface, renderSkFont, skPaint);
        _cache[runProperty] = info;
        return info;
    }

    private readonly Dictionary<SkiaTextRunProperty, RenderingRunPropertyInfo> _cache =
        new Dictionary<SkiaTextRunProperty, RenderingRunPropertyInfo>();

}
