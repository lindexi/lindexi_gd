using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

public class RunProperty : LayoutOnlyRunProperty, IEquatable<RunProperty>, IRunProperty
{
    internal RunProperty(RunPropertyPlatformManager runPropertyPlatformManager, RunProperty? styleRunProperty = null) : base(styleRunProperty)
    {
        RunPropertyPlatformManager = runPropertyPlatformManager;
        StyleRunProperty = styleRunProperty;
    }

    #region 属性

    #region Foreground

    public ImmutableBrush Foreground
    {
        set
        {
            _foreground = value;
        }
        get
        {
            return _foreground ?? StyleRunProperty?.Foreground ?? DefaultForeground;
        }
    }

    public static readonly ImmutableBrush DefaultForeground = new ImmutableBrush(Brushes.Black);

    private ImmutableBrush? _foreground;

    #endregion

    #region Background

    public ImmutableBrush? Background
    {
        set => _background = value;
        get => _background ?? StyleRunProperty?.Background ?? DefaultBackground;
    }

    public static ImmutableBrush? DefaultBackground => null;

    private ImmutableBrush? _background;

    #endregion

    #region Opacity

    public double Opacity
    {
        set => _opacity = value;
        get => _opacity ?? StyleRunProperty?.Opacity ?? DefaultOpacity;
    }

    public const double DefaultOpacity = 1;

    private double? _opacity;

    #endregion

    #region FontStretch

    public FontStretch Stretch
    {
        set => _stretch = value;
        get => _stretch ?? StyleRunProperty?.Stretch ?? DefaultFontStretch;
    }

    private FontStretch? _stretch;
    public static FontStretch DefaultFontStretch => FontStretches.Normal;

    #endregion

    #region FontWeight

    /// <summary>
    /// 字的粗细度，默认值为Normal
    /// </summary>
    public FontWeight FontWeight
    {
        set => _fontWeight = value;
        get => _fontWeight ?? StyleRunProperty?.FontWeight ?? DefaultFontWeight;
    }

    private FontWeight? _fontWeight;
    public static FontWeight DefaultFontWeight => FontWeights.Normal;

    #endregion

    #region FontStyle

    /// <summary>
    /// 斜体表示，默认值为Normal
    /// </summary>
    public FontStyle FontStyle
    {
        set => _fontStyle = value;
        get=> _fontStyle ?? StyleRunProperty?.FontStyle ?? DefaultFontStyle;
    }

    private FontStyle? _fontStyle;
    public static FontStyle DefaultFontStyle => FontStyles.Normal;

    #endregion

    #endregion

    #region 框架

    private RunPropertyPlatformManager RunPropertyPlatformManager { get; }

    /// <summary>
    /// 继承样式里的属性
    /// </summary>
    private RunProperty? StyleRunProperty { get; }

    /// <summary>
    /// 获取渲染使用的字体
    /// </summary>
    /// <param name="unicodeChar">用在找不到字体时，通过将要渲染的字符获取到字体</param>
    /// <returns></returns>
    public GlyphTypeface GetGlyphTypeface(char unicodeChar = '1')
    {
        return _glyphTypeface ??= GetGlyphTypefaceInner();

        GlyphTypeface GetGlyphTypefaceInner()
        {
            // 先判断是否配置了文本的字体等属性，如果没有就从 StyleRunProperty 里面获取。如此可以尽可能复用 StyleRunProperty 的字体
            if (StyleRunProperty is not null)
            {
                if (_stretch is null && _fontWeight is null && FontName.Equals(StyleRunProperty.FontName))
                {
                    // 如果当前的字符属性啥字体相关的都没有设置，那就使用 StyleRunProperty 的，如此可以尽可能复用字体
                    return StyleRunProperty.GetGlyphTypeface();
                }
            }

            return RunPropertyPlatformManager.GetGlyphTypeface(this, unicodeChar);
        }
    }

    private GlyphTypeface? _glyphTypeface;

    #endregion

    #region 相等判断

    public override bool Equals(IReadOnlyRunProperty? other)
    {
        if (ReferenceEquals(other, this))
        {
            // 大部分的判断情况下，都会进入这个分支
            return true;
        }

        if (other is RunProperty otherRunProperty)
        {
            return base.Equals(other) && Equals(otherRunProperty);
        }
        else
        {
            return false;
        }
    }

    public bool Equals(RunProperty? other)
    {
        if (other == null)
        {
            return false;
        }

        // 按照用户可能修改的属性排序，被设置越多的属性放在最前面
        
        if (!Equals(Foreground, other.Foreground))
        {
            return false;
        }
        if (!Equals(Background, other.Background))
        {
            return false;
        }

        if (!Equals(Opacity, other.Opacity))
        {
            return false;
        }

        if (!Equals(Stretch, other.Stretch))
        {
            return false;
        }

        if (!Equals(FontWeight, other.FontWeight))
        {
            return false;
        }

        return true;
    }

    #endregion
}