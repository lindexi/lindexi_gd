using System.Windows;

namespace Pano.Net.DirectX11;

internal sealed class PanoramaCamera
{
    private const double DegreesToRadians = Math.PI / 180;
    private const double MinimumPitch = 0.01;
    private const double MaximumPitch = 179.99;
    private const double MinimumFieldOfView = 1;
    private const double MaximumFieldOfView = 140;

    public double Yaw { get; private set; } = 180;

    public double Pitch { get; private set; } = 90;

    public double FieldOfView { get; private set; } = 100;

    public void Rotate(Vector translation, Size viewport)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        double horizontalFieldOfViewRadians = FieldOfView * DegreesToRadians;
        double verticalFieldOfView = 2
            * Math.Atan(viewport.Height / viewport.Width * Math.Tan(horizontalFieldOfViewRadians / 2))
            / DegreesToRadians;

        Yaw = (Yaw - translation.X * FieldOfView / viewport.Width) % 360;
        if (Yaw < 0)
        {
            Yaw += 360;
        }

        Pitch = Math.Clamp(
            Pitch - translation.Y * verticalFieldOfView / viewport.Height,
            MinimumPitch,
            MaximumPitch);
    }

    public void Zoom(double scale)
    {
        if (scale <= 0)
        {
            return;
        }

        FieldOfView = Math.Clamp(FieldOfView / scale, MinimumFieldOfView, MaximumFieldOfView);
    }
}
