namespace PptxGenerator.Models;

/// <summary>
/// 表示一个纯数据尺寸，无 UI 框架依赖。
/// </summary>
public readonly struct SlideMlSize : IEquatable<SlideMlSize>
{
    /// <summary>
    /// 初始化 <see cref="SlideMlSize"/> 结构的新实例。
    /// </summary>
    public SlideMlSize(double width, double height)
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
    public bool Equals(SlideMlSize other) => Width == other.Width && Height == other.Height;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SlideMlSize other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Width, Height);

    /// <inheritdoc />
    public override string ToString() => $"({Width} x {Height})";

    public static bool operator ==(SlideMlSize left, SlideMlSize right) => left.Equals(right);

    public static bool operator !=(SlideMlSize left, SlideMlSize right) => !left.Equals(right);
}
