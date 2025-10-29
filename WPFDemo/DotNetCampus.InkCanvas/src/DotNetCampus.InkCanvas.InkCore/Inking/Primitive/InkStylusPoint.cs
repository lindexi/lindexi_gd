using DotNetCampus.Numerics.Geometry;

namespace DotNetCampus.Inking.Primitive;

public readonly record struct InkStylusPoint
{
    public InkStylusPoint(Point2D point, float pressure = DefaultPressure)
    {
        Point = point;
        Pressure = pressure;
    }

    public InkStylusPoint(double x, double y, float pressure = DefaultPressure) : this(new Point2D(x, y), pressure)
    {
    }

    public double X => Point.X;
    public double Y => Point.Y;

    public Point2D Point { init; get; }
    public float Pressure { init; get; }

    public bool IsPressureEnable { init; get; }

    public double? Width { init; get; }
    public double? Height { init; get; }

    public const float DefaultPressure = 0.5f;
}