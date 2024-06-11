using Microsoft.Maui.Graphics;

namespace UnoInk.Inking.InkCore;

public readonly record struct StylusPoint(Point Point, float Pressure = 0.5f)
{
    public StylusPoint(double x, double y, float Pressure = 0.5f) : this(new Point(x, y), Pressure)
    {
    }

    public static implicit operator StylusPoint(Point point) => new StylusPoint(point);

    public bool IsPressureEnable { init; get; }

    public double? Width { init; get; }
    public double? Height { init; get; }

    //public static implicit operator StylusPoint(Windows.Foundation.Point point) => new StylusPoint(point.ToPoint());

    //public static implicit operator Windows.Foundation.Point(StylusPoint point) => point.Point.ToFoundationPoint();
}