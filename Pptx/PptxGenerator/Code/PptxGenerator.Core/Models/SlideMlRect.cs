namespace PptxGenerator.Models;

/// <summary>
/// 表示一个纯数据矩形，无 UI 框架依赖。
/// </summary>
public readonly struct SlideMlRect : IEquatable<SlideMlRect>
{
    /// <summary>
    /// 初始化 <see cref="SlideMlRect"/> 结构的新实例。
    /// </summary>
    public SlideMlRect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 矩形左上角 X 坐标。
    /// </summary>
    public double X { get; }

    /// <summary>
    /// 矩形左上角 Y 坐标。
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// 矩形宽度。
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// 矩形高度。
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// 矩形左边界。
    /// </summary>
    public double Left => X;

    /// <summary>
    /// 矩形上边界。
    /// </summary>
    public double Top => Y;

    /// <summary>
    /// 矩形右边界。
    /// </summary>
    public double Right => X + Width;

    /// <summary>
    /// 矩形下边界。
    /// </summary>
    public double Bottom => Y + Height;

    /// <summary>
    /// 矩形中心点 X 坐标。
    /// </summary>
    public double CenterX => X + Width / 2;

    /// <summary>
    /// 矩形中心点 Y 坐标。
    /// </summary>
    public double CenterY => Y + Height / 2;

    /// <inheritdoc />
    public bool Equals(SlideMlRect other)
        => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SlideMlRect other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Width}, {Height})";

    public static bool operator ==(SlideMlRect left, SlideMlRect right) => left.Equals(right);

    public static bool operator !=(SlideMlRect left, SlideMlRect right) => !left.Equals(right);
}
