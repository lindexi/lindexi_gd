namespace FantasticCanvasPlacementExperiment.Lib;

/// <summary>世界坐标点。</summary>
/// <param name="X">横向世界坐标。</param>
/// <param name="Y">纵向世界坐标，向下为正。</param>
public readonly record struct PointD(double X, double Y);

/// <summary>放置前确定的最终尺寸。</summary>
/// <param name="Width">世界坐标宽度，不随视图缩放改变。</param>
/// <param name="Height">世界坐标高度，不随视图缩放改变。</param>
public readonly record struct SizeD(double Width, double Height);

/// <summary>轴对齐世界矩形；边缘接触不视为重叠。</summary>
/// <param name="X">左上角横向世界坐标。</param>
/// <param name="Y">左上角纵向世界坐标，向下为正。</param>
/// <param name="Width">世界坐标宽度。</param>
/// <param name="Height">世界坐标高度。</param>
public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public PointD Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>判断整个矩形是否包含在本矩形内。</summary>
    public bool Contains(RectD other) => other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;

    /// <summary>判断两个矩形的内部是否相交。</summary>
    public bool Overlaps(RectD other) => X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;
}

/// <summary>只计算位置，不改变调用方的视口、尺寸或已有矩形。</summary>
public static class Placement
{
    /// <summary>自动放置容器之间的最小间距，单位为世界坐标。</summary>
    public const double MinimumMargin = 10;

    /// <summary>从实际视口中心开始，按中心行、下上交替的几何事件行和每行先右后左的顺序寻找新容器位置。</summary>
    /// <param name="viewport">不含工具栏的实际视口世界矩形；不会被修改。</param>
    /// <param name="size">新容器的最终布局尺寸，以世界坐标表示，不随视图缩放改变。</param>
    /// <param name="occupied">已有容器的世界边界；集合及其矩形不会被修改，已有容器可彼此重叠。</param>
    /// <returns>新容器左上角的世界坐标，可能位于实际视口之外。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="occupied"/> 为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">坐标不是有限数，或视口、尺寸、已有容器的宽高不是有限正数。</exception>
    /// <exception cref="OverflowException">边界、间距或搜索运算溢出，double 精度无法表示所需尺寸或间距，或扩展不再增长。</exception>
    /// <remarks>
    /// 与已有容器至少保留 10 世界坐标单位间距，范围边缘不要求间距。
    /// 当前逻辑范围无可用位置时，宽高分别乘以 1.2，保持原视口中心继续搜索；只扩大内部逻辑范围，不改变实际视口。
    /// </remarks>
    public static PointD Find(RectD viewport, SizeD size, IReadOnlyList<RectD> occupied)
    {
        ArgumentNullException.ThrowIfNull(occupied);
        Validate(viewport, nameof(viewport));
        Positive(size.Width, nameof(size));
        Positive(size.Height, nameof(size));
        RectD[] obstacles = occupied.ToArray();
        for (int i = 0; i < obstacles.Length; i++)
        {
            Validate(obstacles[i], nameof(occupied));
            obstacles[i] = ExpandObstacle(obstacles[i]);
        }

        double[] rowBuffer = new double[checked(2 * obstacles.Length + 3)];
        double[] columnBuffer = new double[rowBuffer.Length];
        PointD center = viewport.Center;
        double width = viewport.Width;
        double height = viewport.Height;
        RectD range = viewport;
        while (true)
        {
            ValidateArithmetic(range);
            if (size.Width <= width && size.Height <= height)
            {
                double maxX = range.Right - size.Width;
                double maxY = range.Bottom - size.Height;
                // 即使边界会被范围过滤，也必须先完成所有行 Difference 校验，再处理列。
                int rowCount = Events(rowBuffer, obstacles, size.Height, center.Y, range.Y, maxY, true, out int rowSplit);
                int columnCount = Events(columnBuffer, obstacles, size.Width, center.X, range.X, maxX, false, out int columnSplit);
                int above = rowSplit - 1;
                int below = rowSplit;
                bool centerPending = below < rowCount && rowBuffer[below] == center.Y;
                bool nextBelow = true;
                while (above >= 0 || below < rowCount)
                {
                    double y;
                    if (centerPending)
                    {
                        y = rowBuffer[below++];
                        centerPending = false;
                    }
                    else
                    {
                        y = (nextBelow && below < rowCount) || above < 0
                            ? rowBuffer[below++] : rowBuffer[above--];
                        nextBelow = !nextBelow;
                    }

                    for (int column = 0; column < columnCount; column++)
                    {
                        int index = column < columnCount - columnSplit
                            ? columnSplit + column : columnCount - 1 - column;
                        RectD candidate = new(columnBuffer[index], y, size.Width, size.Height);
                        ValidateArithmetic(candidate);
                        if (!range.Contains(candidate)) continue;
                        bool overlaps = false;
                        for (int i = 0; i < obstacles.Length; i++)
                        {
                            if (candidate.Overlaps(obstacles[i]))
                            {
                                overlaps = true;
                                break;
                            }
                        }
                        if (!overlaps) return new(candidate.X, candidate.Y);
                    }
                }
            }

            double nextWidth = width * 1.2;
            double nextHeight = height * 1.2;
            if (!double.IsFinite(nextWidth) || !double.IsFinite(nextHeight) || nextWidth <= width || nextHeight <= height)
                throw new OverflowException();
            width = nextWidth;
            height = nextHeight;
            range = new(center.X - width / 2, center.Y - height / 2, width, height);
        }
    }

    private static int Events(Span<double> buffer, ReadOnlySpan<RectD> obstacles, double length,
        double center, double min, double max, bool vertical, out int split)
    {
        int count = 0;
        for (int i = 0; i < obstacles.Length; i++)
        {
            RectD obstacle = obstacles[i];
            buffer[count++] = Difference(vertical ? obstacle.Y : obstacle.X, length);
            buffer[count++] = vertical ? obstacle.Bottom : obstacle.Right;
        }
        buffer[count++] = center;
        buffer[count++] = min;
        buffer[count++] = max;

        int filtered = 0;
        for (int i = 0; i < count; i++)
        {
            double value = buffer[i];
            if (value >= min && value <= max) buffer[filtered++] = value;
        }
        buffer = buffer[..filtered];
        buffer.Sort();
        int unique = 0;
        split = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            double value = buffer[i];
            if (unique > 0 && value == buffer[unique - 1]) continue;
            buffer[unique++] = value;
            if (value < center) split++;
        }
        return unique;
    }

    private static RectD ExpandObstacle(RectD obstacle)
    {
        RectD expanded = new(obstacle.X - MinimumMargin, obstacle.Y - MinimumMargin,
            obstacle.Width + 2 * MinimumMargin, obstacle.Height + 2 * MinimumMargin);
        ValidateArithmetic(expanded);
        if (obstacle.X - expanded.X < MinimumMargin || obstacle.Y - expanded.Y < MinimumMargin ||
            expanded.Right - obstacle.Right < MinimumMargin || expanded.Bottom - obstacle.Bottom < MinimumMargin)
            throw new OverflowException();
        return expanded;
    }

    private static double Difference(double position, double length)
    {
        double result = position - length;
        if (!double.IsFinite(result)) throw new OverflowException();
        return result;
    }

    private static void Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void Validate(RectD rectangle, string name)
    {
        Positive(rectangle.Width, name);
        Positive(rectangle.Height, name);
        if (!double.IsFinite(rectangle.X) || !double.IsFinite(rectangle.Y)) throw new ArgumentOutOfRangeException(name);
        ValidateArithmetic(rectangle);
    }

    private static void ValidateArithmetic(RectD rectangle)
    {
        if (!double.IsFinite(rectangle.X) || !double.IsFinite(rectangle.Y) ||
            !double.IsFinite(rectangle.Right) || !double.IsFinite(rectangle.Bottom) ||
            rectangle.Right <= rectangle.X || rectangle.Bottom <= rectangle.Y)
            throw new OverflowException();
    }
}
