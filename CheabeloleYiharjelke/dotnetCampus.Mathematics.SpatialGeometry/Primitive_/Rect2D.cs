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
}