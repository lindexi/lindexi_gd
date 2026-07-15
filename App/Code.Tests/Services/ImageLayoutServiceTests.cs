using Avalonia;
using ImageViewer.Services;
using Xunit;

namespace ImageViewer.Tests.Services;

public sealed class ImageLayoutServiceTests
{
    private readonly ImageLayoutService _service = new();

    [Theory]
    [InlineData(90, 40, 75)]
    [InlineData(-90, -40, -75)]
    [InlineData(180, -75, 40)]
    public void WhenPannedImageIsRotatedThenPanOffsetRotatesAroundViewportCenter(
        int degrees,
        double expectedX,
        double expectedY)
    {
        var result = _service.RotatePanOffset(new Point(75, -40), degrees);

        Assert.Equal(expectedX, result.X, 10);
        Assert.Equal(expectedY, result.Y, 10);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(-90)]
    public void WhenPannedImageIsRotatedThenViewportCenterKeepsSameImagePoint(int degrees)
    {
        var panOffset = new Point(75, -40);
        var imagePointAtViewportCenter = _service.RotatePanOffset(
            new Point(-panOffset.X, -panOffset.Y),
            -30);
        var rotatedPanOffset = _service.RotatePanOffset(panOffset, degrees);
        var rotatedImagePoint = _service.RotatePanOffset(imagePointAtViewportCenter, 30 + degrees);

        Assert.Equal(0, rotatedPanOffset.X + rotatedImagePoint.X, 10);
        Assert.Equal(0, rotatedPanOffset.Y + rotatedImagePoint.Y, 10);
    }

    [Theory]
    [InlineData(0, 400, 200)]
    [InlineData(90, 200, 400)]
    [InlineData(180, 400, 200)]
    [InlineData(270, 200, 400)]
    public void WhenImageIsRotatedThenVisualBoundsStayCentered(
        int rotationDegrees,
        double expectedWidth,
        double expectedHeight)
    {
        var layout = _service.CalculateLayout(
            new Size(1000, 800),
            new Size(400, 200),
            1,
            rotationDegrees,
            default);

        Assert.Equal(new Point(500, 400), layout.VisualBounds.Center);
        Assert.Equal(new Size(expectedWidth, expectedHeight), layout.VisualBounds.Size);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void WhenImageIsPannedAndRotatedThenVisualCenterIncludesPanOffset(int rotationDegrees)
    {
        var layout = _service.CalculateLayout(
            new Size(1000, 800),
            new Size(400, 200),
            1.5,
            rotationDegrees,
            new Point(75, -40));

        Assert.Equal(new Point(575, 360), layout.VisualBounds.Center);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void WhenImageIsRotatedThenCanvasPositionKeepsControlCenterFixed(int rotationDegrees)
    {
        var layout = _service.CalculateLayout(
            new Size(1000, 800),
            new Size(400, 200),
            1,
            rotationDegrees,
            default);
        var controlCenter = layout.CanvasPosition + new Vector(
            layout.ImageSize.Width / 2,
            layout.ImageSize.Height / 2);

        Assert.Equal(new Point(500, 400), controlCenter);
    }
}
