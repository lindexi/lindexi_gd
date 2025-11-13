using LightTextEditorPlus.Core.Diagnostics.LogInfos;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Utils;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;

using SkiaSharp;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LightTextEditorPlus.Platform;

/// <summary>
/// Skia 平台资源管理器
/// </summary>
/// - 细分：
///   - 字体资源管理器
///     - 字体缓存
///     - 字体回滚策略
///
/// 字体回滚有两层：
/// 第一层是用户传入的 UserFontName 找不到时的，字体名回滚策略
/// 第二层是传入的字体无法处理对应字符时的回滚策略
public class SkiaPlatformResourceManager :
    //IFontNameToSKTypefaceManager,
    IPlatformFontNameManager
{
    /// <summary>
    /// 创建 Skia 平台资源管理器
    /// </summary>
    /// <param name="textEditor"></param>
    public SkiaPlatformResourceManager(SkiaTextEditor textEditor)
    {
        SkiaTextEditor = textEditor;
        textEditor.InternalRenderCompleted += TextEditor_InternalRenderCompleted;
    }

    /// <summary>
    /// 文本编辑器
    /// </summary>
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

        if (normalRunProperty.IsMissRenderFont)
        {
            normalRunProperty = normalRunProperty with
            {
                IsMissRenderFont = false
            };
        }

        // 使用 TryResolveFont 方法，允许重写，注入程序集的字体
        SKTypeface? skTypeface = TryResolveFont(normalRunProperty);
        bool shouldDisposeSkTypeface = false; // 如果能从 TryResolveFont 找到字体，则此字体不在缓存里面，需要在此上下文内直接释放，减少软泄露
        if (skTypeface is null)
        {
            // 找不到字体，进入字体回滚策略
            string renderFontName = skiaTextRunProperty.RenderFontName;
            string fallbackFontName = GetFallbackFontName(renderFontName);
            normalRunProperty = skiaTextRunProperty with
            {
                RenderFontName = fallbackFontName
            };

            skTypeface = ResolveWithCache(normalRunProperty);
            shouldDisposeSkTypeface = false; // 走 Resolve 会自动加入缓存，会在统一的时机，将整个缓存字典里的资源释放。因此不需要在此上下文释放
        }
        else
        {
            shouldDisposeSkTypeface = true;
        }

        var notContainedChar = charObject.IsLineBreak();
        if (notContainedChar)
        {
            // 如果不包含字符，那么不需要检查字体是否支持字符
            return normalRunProperty;
        }

        Utf32CodePoint codePoint = charObject.CodePoint;

        if (!skTypeface.ContainsGlyph(codePoint.Value))
        {
            SKTypeface? matchCharacterTypeface = TryMatchCharacterTypeface(normalRunProperty, codePoint);

            if (matchCharacterTypeface != null)
            {
                normalRunProperty = normalRunProperty with
                {
                    RenderFontName = matchCharacterTypeface.FamilyName,
                };

                var fontFallbackLogInfo = new CharObjectFontFallbackLogInfo(skiaTextRunProperty.FontName.UserFontName, charObject, normalRunProperty.RenderFontName);
                SkiaTextEditor.Logger.Log(fontFallbackLogInfo);

                //SkiaTextEditor.Logger.LogWarning($"当前字体 '{skiaTextRunProperty.FontName}' 不支持字符 '{charObject.ToText()}'，回滚为 '{normalRunProperty.RenderFontName}' 字体");

                matchCharacterTypeface.Dispose();
            }
            else
            {
                // 暂时不知道咋做，但应该不会遇到
                // 当前就遇到了，传入字符为 \u2001 就在 Skia 3.119.0 版本找不到回滚字体
                SkiaTextEditor.Logger.LogWarning($"无法找到任何字体能够支持字符： '{charObject.ToText()}'，只好设置为错误状态");

                normalRunProperty = normalRunProperty with
                {
                    RenderFontName = skTypeface.FamilyName,
                    IsMissRenderFont = true,
                };
            }
        }

        if (shouldDisposeSkTypeface)
        {
            skTypeface.Dispose();
        }

        return normalRunProperty;
    }

    private readonly string[] _cacheBcp47StringArray = new string[1];

    private SKTypeface? TryMatchCharacterTypeface(SkiaTextRunProperty normalRunProperty, Utf32CodePoint codePoint)
    {
//#if DEBUG
//        // 调试下，强行将此当成找不到字体的情况，方便调试
//        if (codePoint.Value == '\u2001')
//        {
//            return null;
//        }
//#endif

        // 字体不支持此字符。尝试进入字符回滚策略。核心调用的是 MatchCharacter 方法
        using SKFontStyle skFontStyle = normalRunProperty.ToSKFontStyle();

        // 先测试带 bcp47 的 MatchCharacter 方法
        string[]? bcp47 = _cacheBcp47StringArray;
        _cacheBcp47StringArray[0] = SkiaTextEditor.TextEditorCore.CurrentCulture.Name ;

        SKTypeface? matchCharacterTypeface = SKFontManager.Default.MatchCharacter(normalRunProperty.RenderFontName,
            skFontStyle, bcp47, codePoint.Value);

        if (matchCharacterTypeface is null)
        {
            bcp47 = null;
            matchCharacterTypeface = SKFontManager.Default.MatchCharacter(normalRunProperty.RenderFontName,
                skFontStyle, bcp47, codePoint.Value);
        }

        if (matchCharacterTypeface is null)
        {
            matchCharacterTypeface = SKFontManager.Default.MatchCharacter(codePoint.Value);
        }

        if (matchCharacterTypeface is null)
        {
            foreach (string fontFamily in SKFontManager.Default.FontFamilies)
            {
                using SKTypeface matchFamily = SKFontManager.Default.MatchFamily(fontFamily);
                if (matchFamily.ContainsGlyph(codePoint.Value))
                {
                    matchCharacterTypeface = matchFamily;
                    break;
                }
            }
        }

        return matchCharacterTypeface;
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
            skTypeface = ResolveWithCache(runProperty);
        }

        if (SkiaTextEditor.TextEditorCore.IsInDebugMode)
        {
            // 判断字体是否支持字符本身是要钱的，所以只在调试模式下才判断
            if (!skTypeface.ContainsGlyph(codePointToDebug.Value))
            {
                SkiaTextEditor.Logger.LogWarning($"当前字体 {runProperty.FontName} 不支持字符 {codePointToDebug}");
            }
        }

        double fontSize = runProperty.GetRenderFontSize();

        SKFont renderSkFont = new SKFont(skTypeface, (float) fontSize);
        // From Avalonia
        // Ideally the requested edging should be passed to the glyph run.
        // Currently the edging is computed dynamically inside the drawing context, so we can't know it in advance.
        // But the bounds depends on the edging: for now, always use SubpixelAntialias so we have consistent values.
        // The resulting bounds may be shifted by 1px on some fonts:
        // "F" text with Inter size 14 has a 0px left bound with SubpixelAntialias but 1px with Antialias.
        // 在龙芯设备上，如果使用 SubpixelAntialias 会导致文本被相互盖住
        var edging = SKFontEdging.SubpixelAntialias;

        renderSkFont.Hinting = SKFontHinting.Full;
        renderSkFont.Edging = edging;
        renderSkFont.Subpixel = edging != SKFontEdging.Alias;

        SKPaint skPaint = new SKPaint(renderSkFont);
        // skPaint 已经用上 SKFont 的字号属性，不需要再设置 TextSize 属性
        //skPaint.TextSize = runProperty.FontSize;

        // 由于现在文本前景色支持其他画刷，不能在此过程中直接设置。如渐变色需要知道渲染范围才能设置，因此在更后面处理过程中，再设置颜色
        //skPaint.Color = runProperty.Foreground;
        //if (runProperty.Opacity < 1)
        //{
        //    // 处理透明度
        //    skPaint.Color = skPaint.Color.WithAlpha((byte) (skPaint.Color.Alpha * runProperty.Opacity));
        //}

        skPaint.IsAntialias = true;

        var info = new RenderingRunPropertyInfo(skTypeface, renderSkFont, skPaint);
        _cache[runProperty] = new CacheRenderingRunPropertyInfo(skTypeface, renderSkFont, skPaint);
        return info;
    }

    private readonly Dictionary<SkiaTextRunProperty, CacheRenderingRunPropertyInfo> _cache =
        new Dictionary<SkiaTextRunProperty, CacheRenderingRunPropertyInfo>();

    /// <summary>
    /// 解析字体
    /// </summary>
    /// <param name="runProperty"></param>
    /// <returns></returns>
    private SKTypeface ResolveWithCache(SkiaTextRunProperty runProperty)
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

    /// <summary>
    /// 尝试根据字体名解析字体。如果找不到字体，返回 null 空值
    /// </summary>
    /// <param name="runProperty"></param>
    /// <returns>返回空等于字体没有安装</returns>
    private SKTypeface? TryResolveFont(SkiaTextRunProperty runProperty)
    {
        string fontName = runProperty.RenderFontName;
        using SKFontStyle skFontStyle = runProperty.ToSKFontStyle();

        var typeface = TryResolveFont(fontName, skFontStyle);
        return typeface;
    }

    /// <summary>
    /// 尝试从传入的参数转换出 SKTypeface 字体信息
    /// </summary>
    /// <param name="fontName"></param>
    /// <param name="skFontStyle"></param>
    /// <returns></returns>
    protected virtual SKTypeface? TryResolveFont(string fontName, SKFontStyle skFontStyle)
    {
        var typeface = SKFontManager.Default.MatchFamily(fontName, skFontStyle);
        if (typeface != null)
        {
            return typeface;
        }

        // 正常不会走到这个分支，因为前置 NormalRunProperty 已确保字体存在且适配
        // 什么情况下会走到这个分支？字体存在，但是某些 SKFontStyle 不支持

        // 如果带字体样式的字体不存在，那么使用默认字体样式
        typeface = SKFontManager.Default.MatchFamily(fontName);
        if (typeface != null)
        {
            return typeface;
        }

        // 正常一定不会走到这个分支，因为前置 NormalRunProperty 已确保字体存在且适配
        // 除非是继承类型乱传

        return null;
    }

    /// <inheritdoc />
    public string GetFallbackDefaultFontName()
    {
        return GetDefaultFontName();
    }

    /// <inheritdoc />
    public string GetFallbackFontName(string desiredFontName)
    {
        FontFallbackInfo info = TextContext.GlobalFontNameManager.GetFallbackFontInfo(desiredFontName, this);

        if (info.IsFallback || info.IsFallbackFailed)
        {
            SkiaTextEditor.Logger.Log(new FontNameFallbackLogInfo(desiredFontName, info));
        }

        return info.FallbackFontName;
    }

    /// <summary>
    /// 获取默认字名
    /// </summary>
    /// <returns></returns>
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
        new();
}
