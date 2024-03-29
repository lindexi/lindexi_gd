﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

public class RunProperty : LayoutOnlyRunProperty, IEquatable<RunProperty>
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

    private FontStretch ? _stretch;
    public static FontStretch DefaultFontStretch => FontStretches.Normal;

    #endregion

    #region FontWeight

    public FontWeight Weight
    {
        set => _weight = value;
        get => _weight ?? StyleRunProperty?.Weight ?? DefaultWeight;
    }

    private FontWeight? _weight;
    public static FontWeight DefaultWeight => FontWeights.Normal;

    #endregion

    #endregion

    #region 框架

    private RunPropertyPlatformManager RunPropertyPlatformManager { get; }

    /// <summary>
    /// 继承样式里的属性
    /// </summary>
    private RunProperty? StyleRunProperty { get; }

    public GlyphTypeface GetGlyphTypeface()
    {
        return _glyphTypeface ??= GetGlyphTypefaceInner();

        GlyphTypeface GetGlyphTypefaceInner()
        {
            // todo 字体回滚，字体缓存
            FontFamily fontFamily;
            if (FontName.IsNotDefineFontName)
            {
                fontFamily = new FontFamily("微软雅黑");
            }
            else
            {
                fontFamily = new FontFamily(FontName.UserFontName);
            }

            var collection = fontFamily.GetTypefaces();
            Typeface typeface = collection.First();

            foreach (var t in collection)
            {
                if (t.Stretch == Stretch && t.Weight == Weight)
                {
                    typeface = t;
                    break;
                }
            }

            bool success = typeface.TryGetGlyphTypeface(out var glyphTypeface);
            return glyphTypeface;
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

        if (!Equals(Weight, other.Weight))
        {
            return false;
        }

        return true;
    }

    #endregion
}