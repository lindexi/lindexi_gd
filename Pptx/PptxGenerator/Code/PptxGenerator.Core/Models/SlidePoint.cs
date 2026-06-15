using System;

namespace PptxGenerator;

/// <summary>
/// 表示一个纯数据点，无 UI 框架依赖。
/// </summary>
public readonly struct SlidePoint : IEquatable<SlidePoint>
{
    /// <summary>
    /// 初始化 <see cref="SlidePoint"/> 结构的新实例。
    /// </summary>
    public SlidePoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// 点的 X 坐标。
    /// </summary>
    public double X { get; }

    /// <summary>
    /// 点的 Y 坐标。
    /// </summary>
    public double Y { get; }

    /// <inheritdoc />
    public bool Equals(SlidePoint other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SlidePoint other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y})";

    public static bool operator ==(SlidePoint left, SlidePoint right) => left.Equals(right);

    public static bool operator !=(SlidePoint left, SlidePoint right) => !left.Equals(right);
}