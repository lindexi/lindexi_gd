using WpfInk.PresentationCore.System.Windows.Input.Stylus;

namespace WpfInk;

public readonly record struct InkStylusPoint2D(double X, double Y, float Pressure = StylusPoint.DefaultPressure)
{
    public InkStylusPoint2D(InkPoint2D point) : this(point.X, point.Y)
    {
    }
}