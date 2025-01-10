using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using HarfBuzzSharp;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Patterns;
using LightTextEditorPlus.Platform;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

public class SkiaPlatformResourceManager : IFontNameToSKTypefaceManager, IPlatformFontNameManager
{
    public SkiaPlatformResourceManager(SkiaTextEditor textEditor)
    {
        SkiaTextEditor = textEditor;
        textEditor.InternalRenderCompleted += TextEditor_InternalRenderCompleted;
    }

    public SkiaTextEditor SkiaTextEditor { get; }

    //public SkiaTextEditorPlatformProvider PlatformProvider => _skiaTextEditor.SkiaTextEditorPlatformProvider;

    private void TextEditor_InternalRenderCompleted(object? sender, EventArgs e)
    {
        // 每次渲染完成，都可以清理缓存。只有渲染布局过程才会需要用到 Skia 资源
        foreach (KeyValuePair<SkiaTextRunProperty, CacheRenderingRunPropertyInfo> renderingFontInfo in _cache)
        {
            var runPropertyInfo = renderingFontInfo.Value;
            runPropertyInfo.Dispose();
        }

        _cache.Clear();
        _cache.TrimExcess();
    }

    /// <summary>
    /// 规范化字符属性。确保使用的字体支持字符和字体存在
    /// </summary>
    /// <param name="skiaTextRunProperty"></param>
    /// <param name="charObject"></param>
    /// <returns></returns>
    public IReadOnlyRunProperty NormalRunProperty(SkiaTextRunProperty skiaTextRunProperty, ICharObject charObject)
    {
        // 1. 先确定字体是否存在或装上
        // 2. 确定字体是否支持字符
        var normalRunProperty = skiaTextRunProperty;
        // 使用 TryResolveFont 方法，允许重写，注入程序集的字体
        SKTypeface? skTypeface = TryResolveFont(normalRunProperty);
        bool shouldDisposeSkTypeface = false;
        if (skTypeface is null)
        {
            // 找不到字体，进入字体回滚策略
            string renderFontName = skiaTextRunProperty.RenderFontName;
            string fallbackFontName = SkiaTextEditor.TextEditorCore.FontNameManager.GetFallbackFontName(renderFontName, SkiaTextEditor.TextEditorCore);
            normalRunProperty = skiaTextRunProperty with
            {
                RenderFontName = fallbackFontName
            };

            skTypeface = Resolve(normalRunProperty);
        }
        else
        {
            shouldDisposeSkTypeface = true;
        }

        // todo 给 ICharObject 添加包含字符的判断属性
        var containedChar = charObject is not LineBreakCharObject;
        if (!containedChar)
        {
            // 如果不包含字符，那么不需要检查字体是否支持字符
            return normalRunProperty;
        }

        Utf32CodePoint codePoint = charObject.CodePoint;

        if (!skTypeface.ContainsGlyph(codePoint.Value))
        {
            // 字体不支持字符
            using SKFontStyle skFontStyle = normalRunProperty.ToSKFontStyle();

            using SKTypeface matchCharacter = SKFontManager.Default.MatchCharacter(normalRunProperty.RenderFontName, skFontStyle,null, codePoint.Value);

            normalRunProperty = normalRunProperty with
            {
                RenderFontName = matchCharacter.FamilyName,
            };

            SkiaTextEditor.Logger.LogWarning($"当前字体 '{skiaTextRunProperty.FontName}' 不支持字符 '{charObject.ToText()}'，回滚为 '{normalRunProperty.RenderFontName}' 字体");
        }

        if (shouldDisposeSkTypeface)
        {
            skTypeface.Dispose();
        }

        return normalRunProperty;
    }

    /// <summary>
    /// 获取渲染字符属性信息
    /// </summary>
    /// <param name="runProperty"></param>
    /// <param name="codePointToDebug">这是一个只有调试作用的参数，确保给定的渲染字符属性是能够支持当前字符的，即字体能够支持当前字符的渲染</param>
    /// <returns></returns>
    internal RenderingRunPropertyInfo GetRenderingRunPropertyInfo(SkiaTextRunProperty runProperty, Utf32CodePoint codePointToDebug)
    {
        SKTypeface skTypeface;
        if (_cache.TryGetValue(runProperty, out var cache))
        {
            // 不需要在这里处理找不到字体的情况
            // 因为在 NormalRunProperty 方法里面已经处理了
            if (cache.Font != null && cache.Paint != null)
            {
                return new RenderingRunPropertyInfo(cache.Typeface, cache.Font, cache.Paint);
            }

            skTypeface = cache.Typeface;
        }
        else
        {
            skTypeface = Resolve(runProperty);
        }

        if(SkiaTextEditor.IsInDebugMode)
        {
            // 判断字体是否支持字符本身是要钱的，所以只在调试模式下才判断
            if (!skTypeface.ContainsGlyph(codePointToDebug.Value))
            {
                SkiaTextEditor.Logger.LogWarning($"当前字体 {runProperty.FontName} 不支持字符 {codePointToDebug}");
            }
        }

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
        _cache[runProperty] = new CacheRenderingRunPropertyInfo(skTypeface, renderSkFont, skPaint);
        return info;
    }

    private readonly Dictionary<SkiaTextRunProperty, CacheRenderingRunPropertyInfo> _cache =
        new Dictionary<SkiaTextRunProperty, CacheRenderingRunPropertyInfo>();

    /// <inheritdoc />
    public SKTypeface Resolve(SkiaTextRunProperty runProperty)
    {
        if (_cache.TryGetValue(runProperty, out var cache))
        {
            return cache.Typeface;
        }
        string fontName = runProperty.RenderFontName;
        using SKFontStyle skFontStyle = runProperty.ToSKFontStyle();

        var typeface = TryResolveFont(fontName, skFontStyle);
        if (typeface == null)
        {
            // 测试 SKTypeface.FromFamilyName 性能
            // 测试： e3bfd66963fc551b56ada90d29318ef59de1927b
            // Ave:0.009658386363636364ms
            // 调用 SKTypeface.FromFamilyName 差不多百分之一毫秒
            typeface = SKTypeface.FromFamilyName(fontName, skFontStyle);
        }

        _cache[runProperty] = new CacheRenderingRunPropertyInfo(typeface, null, null);
        return typeface;
    }

    private SKTypeface? TryResolveFont(SkiaTextRunProperty runProperty)
    {
        string fontName = runProperty.RenderFontName;
        using SKFontStyle skFontStyle = runProperty.ToSKFontStyle();

        var typeface = TryResolveFont(fontName, skFontStyle);
        return typeface;
    }

    protected virtual SKTypeface? TryResolveFont(string fontName, SKFontStyle skFontStyle)
    {
        var typeface = SKFontManager.Default.MatchFamily(fontName, skFontStyle);
        if (typeface != null)
        {
            return typeface;
        }

        // 正常不会走到这个分支，因为前置已确保字体存在且适配

        // 如果带字体样式的字体不存在，那么使用默认字体样式
        typeface = SKFontManager.Default.MatchFamily(fontName);
        if (typeface != null)
        {
            return typeface;
        }

        return null;
    }

    /// <inheritdoc />
    public string GetFallbackDefaultFontName()
    {
        return GetDefaultFontName();
    }

    public static string GetDefaultFontName()
    {
        var defaultFontName = "微软雅黑";

        if (OperatingSystem.IsWindows())
        {
            defaultFontName = "微软雅黑";
        }
        else if (OperatingSystem.IsLinux())
        {
            defaultFontName = "Noto Sans CJK SC";
        }
        else if (OperatingSystem.IsMacOS())
        {
            defaultFontName = "PingFang SC";
        }

        return defaultFontName;
    }

    /// <inheritdoc />
    public bool CheckFontFamilyInstalled(string fontName)
    {
        if (InstalledFontCache.TryGetValue(fontName, out var installed))
        {
            return installed;
        }

        SKTypeface typeface = SKFontManager.Default.MatchFamily(fontName);
        installed = typeface != null;
        typeface?.Dispose();
        InstalledFontCache[fontName] = installed;
        return installed;
    }

    /// <summary>
    /// 已安装的字体缓存
    /// </summary>
    /// 字体是有限的，所以不需要担心缓存过大
    private static readonly ConcurrentDictionary<string /*FontName*/, bool /*Installed*/> InstalledFontCache =
        new ();
}
