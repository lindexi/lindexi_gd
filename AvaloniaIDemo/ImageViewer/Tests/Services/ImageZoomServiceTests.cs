using Avalonia;
using ImageViewer.Services;

namespace ImageViewer.Tests.Services;

public sealed class ImageZoomServiceTests
{
    [Theory]
    [InlineData(0.01, 0.10)]
    [InlineData(0.10, 0.10)]
    [InlineData(1.25, 1.25)]
    [InlineData(32.00, 32.00)]
    [InlineData(40.00, 32.00)]
    public void ClampZoomRestrictsZoomToSupportedRange(double requestedZoom, double expectedZoom)
    {
        var service = new ImageZoomService();

        var zoom = service.ClampZoom(requestedZoom);

        Assert.Equal(expectedZoom, zoom);
    }

    [Fact]
    public void CalculateFitZoomUsesSmallestViewportRatio()
    {
        var service = new ImageZoomService();

        var zoom = service.CalculateFitZoom(new Size(800, 600), new Size(1600, 600), 0);

        Assert.Equal(0.5, zoom, precision: 10);
    }

    [Fact]
    public void CalculateFitZoomUsesRotatedBoundsForQuarterTurn()
    {
        var service = new ImageZoomService();

        var zoom = service.CalculateFitZoom(new Size(800, 600), new Size(300, 1200), 90);

        Assert.Equal(2d / 3d, zoom, precision: 10);
    }

    [Theory]
    [InlineData(0, 400, 200)]
    [InlineData(90, 200, 400)]
    [InlineData(180, 400, 200)]
    [InlineData(270, 200, 400)]
    [InlineData(-90, 200, 400)]
    [InlineData(45, 400, 200)]
    [InlineData(450, 200, 400)]
    public void GetRotatedBoundsSizeAccountsForRotation(int rotationDegrees, double expectedWidth, double expectedHeight)
    {
        var service = new ImageZoomService();

        var bounds = service.GetRotatedBoundsSize(new Size(400, 200), 1, rotationDegrees);

        Assert.Equal(new Size(expectedWidth, expectedHeight), bounds);
    }

    [Fact]
    public void CalculateFitZoomReturnsOneForInvalidViewportOrImageSize()
    {
        var service = new ImageZoomService();

        var zoom = service.CalculateFitZoom(default, new Size(400, 200), 0);

        Assert.Equal(1, zoom, precision: 10);
    }
}
