using Windows.Foundation;

namespace UnoInk;

public readonly record struct StrokePoint(Point Point, float Pressure = 0.5f)
{
    public static implicit operator StrokePoint(Point point) => new StrokePoint(point);
}