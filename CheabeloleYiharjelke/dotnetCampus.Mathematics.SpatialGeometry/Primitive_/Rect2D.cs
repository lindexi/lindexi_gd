namespace dotnetCampus.Mathematics.SpatialGeometry;

public readonly record struct Rect2D(Point2D Position, Size2D Size)
{
    public Rect2D(double x, double y, double width, double height) : this(new Point2D(x, y), new Size2D(width, height))
    {
    }

    public double X => Position.X;
    public double Y => Position.Y;

    public double Width => Size.Width;
    public double Height => Size.Height;

    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public static Rect2D Zero => default;

    public static Rect2D FromLTRB(double left, double top, double right, double bottom)
    {
        return new Rect2D(left, top, right - left, bottom - top);
    }

    public bool Contains(Rect2D rect)
    {
        return X <= rect.X && Right >= rect.Right && Y <= rect.Y && Bottom >= rect.Bottom;
    }

    public bool Contains(Point2D pt)
    {
        return Contains(pt.X, pt.Y);
    }

    public bool Contains(double x, double y)
    {
        return (x >= Left) && (x < Right) && (y >= Top) && (y < Bottom);
    }

    public bool IntersectsWith(Rect2D r)
    {
        return !((Left >= r.Right) || (Right <= r.Left) || (Top >= r.Bottom) || (Bottom <= r.Top));
    }

    public Rect2D Union(Rect2D r)
    {
        return Union(this, r);
    }

    public static Rect2D Union(Rect2D r1, Rect2D r2)
    {
        return FromLTRB(Math.Min(r1.Left, r2.Left), Math.Min(r1.Top, r2.Top), Math.Max(r1.Right, r2.Right), Math.Max(r1.Bottom, r2.Bottom));
    }
}