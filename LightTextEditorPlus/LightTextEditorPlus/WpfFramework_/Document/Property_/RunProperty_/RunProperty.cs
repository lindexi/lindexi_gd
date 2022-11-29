using System;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Utils;

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

    #endregion

    #region 框架

    private RunPropertyPlatformManager RunPropertyPlatformManager { get; }

    /// <summary>
    /// 继承样式里的属性
    /// </summary>
    private RunProperty? StyleRunProperty { get; }

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

        return true;
    }

    #endregion

}

/// <summary>
/// 表示一个不可变的画刷
/// </summary>
/// 要是还有人去拿属性去改，那我也救不了了
public class ImmutableBrush : ImmutableRunPropertyValue<Brush>, IEquatable<ImmutableBrush>
{
    public ImmutableBrush(Brush value) : base(value)
    {
    }

    public bool Equals(ImmutableBrush? other)
    {
        if (other is null)
        {
            return false;
        }

        return Converter.AreEquals(this.Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ImmutableBrush) obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}