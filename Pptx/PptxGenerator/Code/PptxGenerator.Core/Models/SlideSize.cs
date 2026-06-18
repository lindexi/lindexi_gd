using System;

namespace PptxGenerator;

/// <summary>
/// 表示一个纯数据尺寸，无 UI 框架依赖。
/// </summary>
public readonly struct SlideSize : IEquatable<SlideSize>
{
    /// <summary>
    /// 初始化 <see cref="SlideSize"/> 结构的新实例。
    /// </summary>
    public SlideSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 宽度。
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// 高度。
    /// </summary>
    public double Height { get; }

    /// <inheritdoc />
    public bool Equals(SlideSize other) => Width == other.Width && Height == other.Height;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SlideSize other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Width, Height);

    /// <inheritdoc />
    public override string ToString() => $"({Width} x {Height})";

    public static bool operator ==(SlideSize left, SlideSize right) => left.Equals(right);

    public static bool operator !=(SlideSize left, SlideSize right) => !left.Equals(right);
}