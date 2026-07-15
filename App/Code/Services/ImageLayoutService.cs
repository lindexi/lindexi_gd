using Avalonia;
using System;

namespace ImageViewer.Services;

internal sealed class ImageLayoutService
{
    private readonly ImageZoomService _zoomService = new();

    public Point RotatePanOffset(Point panOffset, int degrees)
    {
        var radians = degrees * Math.PI / 180;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);

        return new Point(
            panOffset.X * cosine - panOffset.Y * sine,
            panOffset.X * sine + panOffset.Y * cosine);
    }

    public ImageLayout CalculateLayout(
        Size viewportSize,
        Size imagePixelSize,
        double zoom,
        int rotationDegrees,
        Point panOffset)
    {
        var imageSize = _zoomService.GetUnrotatedImageSize(imagePixelSize, zoom);
        var rotatedSize = _zoomService.GetRotatedBoundsSize(imagePixelSize, zoom, rotationDegrees);
        var center = new Point(
            viewportSize.Width / 2 + panOffset.X,
            viewportSize.Height / 2 + panOffset.Y);

        return new ImageLayout(
            imageSize,
            new Point(center.X - imageSize.Width / 2, center.Y - imageSize.Height / 2),
            new Rect(
                center.X - rotatedSize.Width / 2,
                center.Y - rotatedSize.Height / 2,
                rotatedSize.Width,
                rotatedSize.Height));
    }
}

internal readonly record struct ImageLayout(Size ImageSize, Point CanvasPosition, Rect VisualBounds);
