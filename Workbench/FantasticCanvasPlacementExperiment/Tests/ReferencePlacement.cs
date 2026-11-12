using FantasticCanvasPlacementExperiment.Lib;

namespace FantasticCanvasPlacementExperiment.Tests;

// 保留优化前的实现，独立作为结果及分配对照，不调用优化后的搜索逻辑。
internal sealed record ReferenceResult(RectD Rectangle, RectD SearchRange, int ExpansionCount);

internal static class ReferencePlacement
{
    private const double MinimumMargin = 10;

    internal static ReferenceResult Find(RectD viewport, SizeD size, IReadOnlyList<RectD> occupied)
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

        PointD center = viewport.Center;
        double width = viewport.Width;
        double height = viewport.Height;
        int expansions = 0;
        while (true)
        {
            RectD range = expansions == 0 ? viewport : new(center.X - width / 2, center.Y - height / 2, width, height);
            ValidateArithmetic(range);
            if (size.Width <= width && size.Height <= height)
            {
                double maxX = range.Right - size.Width;
                double maxY = range.Bottom - size.Height;
                double[] rows = Events(center.Y, range.Y, maxY,
                    obstacles.SelectMany(o => new[] { Difference(o.Y, size.Height), o.Bottom }));
                double[] columns = Events(center.X, range.X, maxX,
                    obstacles.SelectMany(o => new[] { Difference(o.X, size.Width), o.Right }));
                foreach (double y in AlternateRows(rows, center.Y))
                {
                    foreach (double x in columns.Where(x => x >= center.X).Order()
                                 .Concat(columns.Where(x => x < center.X).OrderDescending()))
                    {
                        RectD candidate = new(x, y, size.Width, size.Height);
                        ValidateArithmetic(candidate);
                        if (range.Contains(candidate) && !obstacles.Any(candidate.Overlaps))
                            return new(candidate, range, expansions);
                    }
                }
            }

            double nextWidth = width * 1.2;
            double nextHeight = height * 1.2;
            if (!double.IsFinite(nextWidth) || !double.IsFinite(nextHeight) || nextWidth <= width || nextHeight <= height)
                throw new OverflowException();
            width = nextWidth;
            height = nextHeight;
            expansions = checked(expansions + 1);
        }
    }

    private static double[] Events(double center, double min, double max, IEnumerable<double> boundaries) =>
        boundaries.Append(center).Append(min).Append(max)
            .Where(value => value >= min && value <= max).Distinct().ToArray();

    private static IEnumerable<double> AlternateRows(double[] rows, double center)
    {
        if (rows.Contains(center))
            yield return center;
        double[] below = rows.Where(y => y > center).Order().ToArray();
        double[] above = rows.Where(y => y < center).OrderDescending().ToArray();
        for (int i = 0; i < Math.Max(below.Length, above.Length); i++)
        {
            if (i < below.Length) yield return below[i];
            if (i < above.Length) yield return above[i];
        }
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
