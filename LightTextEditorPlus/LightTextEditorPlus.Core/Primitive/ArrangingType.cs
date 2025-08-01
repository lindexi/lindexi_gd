using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 布局方式
/// </summary>
public readonly struct ArrangingType : IEquatable<ArrangingType>
{
    /// <summary>
    /// 横排布局
    /// </summary>
    /// 即 horizontal writing mode
    public static ArrangingType Horizontal
        => new ArrangingType();

    /// <summary>
    /// 竖排布局。也称 直排，从右到左
    /// </summary>
    /// 即 vertical writing mode
    public static ArrangingType Vertical => new ArrangingType
    {
        IsVertical = true,
        IsLeftToRightVertical = false,
        LatinRotationDegree = DefaultRotationDegree,
        NumberRotationDegree = DefaultRotationDegree,
    };

    /// <summary>
    /// 蒙古文布局，从左到右
    /// </summary>
    public static ArrangingType Mongolian => new ArrangingType
    {
        IsVertical = true,
        // 蒙古文布局，从左到右
        IsLeftToRightVertical = true,
    };

    /// <summary>
    /// 是否横排
    /// </summary>
    public bool IsHorizontal => !IsVertical;

    /// <summary>
    /// 是否竖排
    /// </summary>
    public bool IsVertical { get; private init; }

    /// <summary>
    /// 是否从左到右布局的竖排
    /// </summary>
    public bool IsLeftToRightVertical { get; private init; }

    /// <summary>
    /// 拉丁文字是否需要旋转，旋转角度为是多少，都是90度倍数为主
    /// </summary>
    public int LatinRotationDegree { get; private init; }

    /// <summary>
    /// 数字是否需要旋转
    /// </summary>
    public int NumberRotationDegree { get; private init; }

    /// <summary>
    /// 默认旋转角度 
    /// </summary>
    public const int DefaultRotationDegree = 90;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator ==(ArrangingType a, ArrangingType b)
    {
        return a.Equals(b);
    }

    /// <summary>
    /// 判断是否不相等
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator !=(ArrangingType a, ArrangingType b)
    {
        return !(a == b);
    }

    /// <inheritdoc />
    public bool Equals(ArrangingType other)
    {
        return IsVertical == other.IsVertical && IsLeftToRightVertical == other.IsLeftToRightVertical &&
               LatinRotationDegree == other.LatinRotationDegree && NumberRotationDegree == other.NumberRotationDegree;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ArrangingType other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(IsVertical, IsLeftToRightVertical, LatinRotationDegree, NumberRotationDegree);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsHorizontal)
        {
            return "Horizontal";
        }
        else
        {
            return $"Vertical {base.ToString()}";
        }
    }
}