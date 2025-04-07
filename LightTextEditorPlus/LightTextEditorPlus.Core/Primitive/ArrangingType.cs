using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
///     布局方式
/// </summary>
public readonly struct ArrangingType : IEquatable<ArrangingType>
{
    /// <summary>
    ///     横排布局
    /// </summary>
    /// 即 horizontal writing mode
    public static ArrangingType Horizontal
        => new ArrangingType();

    /// <summary>
    ///     竖排布局。也称 直排
    /// </summary>
    /// 即 vertical writing mode
    public static ArrangingType Vertical => new ArrangingType
    {
        IsVertical = true,
        IsLeftToRightVertical = true,
    };

    /// <summary>
    ///     蒙古文布局，从右到左
    /// </summary>
    public static ArrangingType Mongolian => new ArrangingType
    {
        IsVertical = true,
        // 蒙古文布局，从右到左
        IsLeftToRightVertical = false,
    };

    public bool IsHorizontal => !IsVertical;

    public bool IsVertical { get; private init; }

    public bool IsLeftToRightVertical { get; private init; }

    /// <summary>
    /// 拉丁文字是否需要旋转，旋转角度为是多少，都是90度倍数为主
    /// </summary>
    public int LatinRotationDegree { get; private init; }

    /// <summary>
    /// 数字是否需要旋转
    /// </summary>
    public bool NumberRotationDegree { get; private init; }

    /// <summary>
    /// 默认旋转角度 
    /// </summary>
    public const int DefaultRotationDegree = 90;

    public static bool operator ==(ArrangingType a, ArrangingType b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(ArrangingType a, ArrangingType b)
    {
        return !(a == b);
    }

    public bool Equals(ArrangingType other)
    {
        return IsVertical == other.IsVertical && IsLeftToRightVertical == other.IsLeftToRightVertical && LatinRotationDegree == other.LatinRotationDegree && NumberRotationDegree == other.NumberRotationDegree;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArrangingType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsVertical, IsLeftToRightVertical, LatinRotationDegree, NumberRotationDegree);
    }
}