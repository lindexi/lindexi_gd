using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 文本字符属性
/// </summary>
[APIConstraint("RunProperty.txt")]
public record RunProperty : LayoutOnlyRunProperty, IEquatable<RunProperty>, IRunProperty
{
    internal RunProperty(RunPropertyPlatformManager runPropertyPlatformManager)
    {
        RunPropertyPlatformManager = runPropertyPlatformManager;
    }

    #region 属性

    /// <inheritdoc />
    public override FontName FontName
    {
        get => base.FontName;
        init
        {
            if (!base.FontName.Equals(value))
            {
                _renderingFontInfo = null;
            }

            base.FontName = value;
        }
    }

    #region Foreground

    /// <inheritdoc />
    public ImmutableBrush Foreground
    {
        init
        {
            _foreground = value;
        }
        get
        {
            return _foreground ?? DefaultForeground;
        }
    }

    /// <summary>
    /// 默认前景色
    /// </summary>
    public static readonly ImmutableBrush DefaultForeground = new ImmutableBrush(Brushes.Black);

    private readonly ImmutableBrush? _foreground;

    #endregion

    #region Background

    /// <inheritdoc />
    public ImmutableBrush? Background
    {
        init => _background = value;
        get => _background ?? DefaultBackground;
    }

    /// <summary>
    /// 默认背景色
    /// </summary>
    public static ImmutableBrush? DefaultBackground => null;

    private readonly ImmutableBrush? _background;

    #endregion

    #region Opacity

    /// <inheritdoc />
    public double Opacity
    {
        init => _opacity = value;
        get => _opacity ?? DefaultOpacity;
    }

    /// <summary>
    /// 默认不透明度
    /// </summary>
    public const double DefaultOpacity = 1;

    private readonly double? _opacity;

    #endregion

    #region FontStretch

    /// <inheritdoc />
    public FontStretch Stretch
    {
        init => _stretch = value;
        get => _stretch ?? DefaultFontStretch;
    }

    private readonly FontStretch? _stretch;
    /// <summary>
    /// 默认字体拉伸
    /// </summary>
    public static FontStretch DefaultFontStretch => FontStretches.Normal;

    #endregion

    #region FontWeight

    /// <summary>
    /// 字的粗细度，默认值为Normal
    /// </summary>
    public FontWeight FontWeight
    {
        init => _fontWeight = value;
        get => _fontWeight ?? DefaultFontWeight;
    }

    private readonly FontWeight? _fontWeight;
    /// <summary>
    /// 默认字重
    /// </summary>
    public static FontWeight DefaultFontWeight => FontWeights.Normal;

    #endregion

    #region FontStyle

    /// <summary>
    /// 斜体表示，默认值为Normal
    /// </summary>
    public FontStyle FontStyle
    {
        init => _fontStyle = value;
        get => _fontStyle ?? DefaultFontStyle;
    }

    private readonly FontStyle? _fontStyle;
    /// <summary>
    /// 默认字体样式
    /// </summary>
    public static FontStyle DefaultFontStyle => FontStyles.Normal;

    #endregion

    #endregion

    #region 框架

    private RunPropertyPlatformManager RunPropertyPlatformManager { get; }

    /// <summary>
    /// 缓存的回滚字体
    /// </summary>
    /// 对于一个字体，如果给定的文本不是当前字体能支持的，一般来说都会存在多个文本都有此问题。于是缓存起来，可以重复使用，减少回滚次数
    private GlyphTypeface? _cacheFallbackGlyphTypeface;

    /// <summary>
    /// 尝试获取渲染时的回滚字体
    /// </summary>
    /// <param name="c"></param>
    /// <param name="glyphTypeface"></param>
    /// <param name="glyphIndex"></param>
    /// <returns></returns>
    public bool TryGetFallbackGlyphTypeface(Utf32CodePoint c, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface,
        out ushort glyphIndex)
    {
        if (_cacheFallbackGlyphTypeface is not null)
        {
            if (_cacheFallbackGlyphTypeface.CharacterToGlyphMap.TryGetValue(c.Value, out var index))
            {
                // 命中缓存
                glyphTypeface = _cacheFallbackGlyphTypeface;
                glyphIndex = index;
                return true;
            }
        }

        if (RunPropertyPlatformManager.TryGetFallbackFontInfoByWpf(this, c, out var result, out _))
        {
            if (result.CharacterToGlyphMap.TryGetValue(c.Value, out var index))
            {
                glyphTypeface = result;
                glyphIndex = index;
                _cacheFallbackGlyphTypeface = glyphTypeface;
                return true;
            }
        }

        glyphTypeface = null;
        glyphIndex = 0;
        return false;
    }

    /// <summary>
    /// 获取渲染使用的字体
    /// </summary>
    /// <param name="unicodeChar"></param>
    /// <returns></returns>
    public FontFamily GetRenderingFontFamily(char unicodeChar = '1')
    {
        return GetRenderingFontFamily(new Utf32CodePoint(unicodeChar));
    }

    /// <summary>
    /// 获取渲染使用的字体
    /// </summary>
    public FontFamily GetRenderingFontFamily(Utf32CodePoint unicodeChar)
    {
        return GetGlyphTypefaceAndRenderingFontFamily(unicodeChar).RenderingFontFamily;
    }

    /// <summary>
    /// 获取渲染使用的字体
    /// </summary>
    /// <param name="unicodeChar">用在找不到字体时，通过将要渲染的字符获取到字体</param>
    /// <returns></returns>
    public GlyphTypeface GetGlyphTypeface(char unicodeChar = '1')
    {
        return GetGlyphTypeface(new Utf32CodePoint(unicodeChar));
    }

    /// <summary>
    /// 获取渲染使用的字体
    /// </summary>
    public GlyphTypeface GetGlyphTypeface(Utf32CodePoint unicodeChar)
    {
        return GetGlyphTypefaceAndRenderingFontFamily(unicodeChar).GlyphTypeface;
    }

    private RenderingFontInfo GetGlyphTypefaceAndRenderingFontFamily(Utf32CodePoint unicodeChar)
    {
        if (_renderingFontInfo is not null)
        {
            return _renderingFontInfo.Value;
        }

        _renderingFontInfo =
            RunPropertyPlatformManager.GetGlyphTypefaceAndRenderingFontFamily(this, unicodeChar);
        return _renderingFontInfo.Value;
    }

    private RenderingFontInfo? _renderingFontInfo;

    #endregion

    //#region 相等判断

    ///// <inheritdoc />
    //public override bool Equals(IReadOnlyRunProperty? other)
    //{
    //    if (ReferenceEquals(other, this))
    //    {
    //        // 大部分的判断情况下，都会进入这个分支
    //        return true;
    //    }

    //    if (other is RunProperty otherRunProperty)
    //    {
    //        return Equals(otherRunProperty);
    //    }
    //    else
    //    {
    //        return false;
    //    }
    //}

    ///// <inheritdoc />
    //public override int GetHashCode()
    //{
    //    var hashCode = new HashCode();
    //    hashCode.Add(FontName);
    //    hashCode.Add(FontSize);
    //    hashCode.Add(Foreground);
    //    hashCode.Add(Opacity);
    //    hashCode.Add(FontWeight);
    //    hashCode.Add(FontStyle);
    //    hashCode.Add(Stretch);
    //    hashCode.Add(Background);
    //    return hashCode.ToHashCode();
    //}

    ///// <inheritdoc />
    //public virtual bool Equals(RunProperty? other)
    //{
    //    if (other == null)
    //    {
    //        return false;
    //    }

    //    // 按照用户可能修改的属性排序，被设置越多的属性放在最前面
    //    if (!base.Equals(other))
    //    {
    //        return false;
    //    }

    //    if (!Equals(Foreground, other.Foreground))
    //    {
    //        return false;
    //    }

    //    if (!Equals(Opacity, other.Opacity))
    //    {
    //        return false;
    //    }

    //    if (!Equals(FontWeight, other.FontWeight))
    //    {
    //        return false;
    //    }

    //    if (!Equals(FontStyle, other.FontStyle))
    //    {
    //        return false;
    //    }

    //    if (!Equals(Stretch, other.Stretch))
    //    {
    //        return false;
    //    }

    //    if (!Equals(Background, other.Background))
    //    {
    //        return false;
    //    }

    //    return true;
    //}

    //#endregion
}
