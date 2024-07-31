namespace dotnetCampus.Mathematics.SpatialGeometry;

public readonly record struct Rect2D(Point2D Position, Size2D Size)
{
    public double X => Position.X;
    public double Y => Position.Y;

    public double Width => Size.Width;
    public double Height => Size.Height;

    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
}