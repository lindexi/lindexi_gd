namespace DotNetCampus.Inking.Utils;

static class PointExtension
{
    public static Avalonia.Point ToAvaloniaPoint(this global::DotNetCampus.Numerics.Geometry.Point2D point)
    {
        return new Avalonia.Point(point.X, point.Y);
    }
}