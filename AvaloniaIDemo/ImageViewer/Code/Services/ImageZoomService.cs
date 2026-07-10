using System;
using Avalonia;

namespace ImageViewer.Services;

internal sealed class ImageZoomService
{
    public const double MinZoom = 0.10;
    public const double MaxZoom = 32.00;

    public double ClampZoom(double zoom)
    {
        return Math.Clamp(zoom, MinZoom, MaxZoom);
    }

    public double CalculateFitZoom(Size viewportSize, Size imageSize, int rotationDegrees)
    {
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return 1;
        }

        var bounds = GetRotatedBoundsSize(imageSize, 1, rotationDegrees);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return 1;
        }

        return ClampZoom(Math.Min(viewportSize.Width / bounds.Width, viewportSize.Height / bounds.Height));
    }

    public Size GetUnrotatedImageSize(Size imageSize, double zoom)
    {
        return new Size(imageSize.Width * zoom, imageSize.Height * zoom);
    }

    public Size GetRotatedBoundsSize(Size imageSize, double zoom, int rotationDegrees)
    {
        var scaledSize = GetUnrotatedImageSize(imageSize, zoom);
        return IsQuarterTurn(rotationDegrees)
            ? new Size(scaledSize.Height, scaledSize.Width)
            : scaledSize;
    }

    public bool IsQuarterTurn(int rotationDegrees)
    {
        var normalizedRotation = ((rotationDegrees % 360) + 360) % 360;
        return normalizedRotation is 90 or 270;
    }
}
