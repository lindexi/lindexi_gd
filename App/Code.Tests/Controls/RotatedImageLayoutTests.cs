using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using ImageViewer.Services;
using System;
using System.Linq;
using Xunit;

namespace ImageViewer.Tests.Controls;

public sealed class RotatedImageLayoutTests
{
    private readonly ImageLayoutService _layoutService = new();

    [AvaloniaFact]
    public void WhenMainWindowIsLoadedThenImageUsesAvaloniaDefaultCenterOrigin()
    {
        var window = new MainWindow();
        var image = window.FindControl<Image>("ViewerImage");

        Assert.NotNull(image);
        Assert.Equal(RelativePoint.Center, image.RenderTransformOrigin);
    }

    [AvaloniaTheory]
    [InlineData(0, 300, 300, 700, 500)]
    [InlineData(90, 400, 200, 600, 600)]
    [InlineData(180, 300, 300, 700, 500)]
    [InlineData(270, 400, 200, 600, 600)]
    public void WhenControlIsRotatedThenTransformedCornersStayCentered(
        int rotationDegrees,
        double expectedLeft,
        double expectedTop,
        double expectedRight,
        double expectedBottom)
    {
        var canvas = new Canvas();
        var window = new Window
        {
            Width = 1000,
            Height = 800,
            Content = canvas
        };
        var image = new Border
        {
            Width = 400,
            Height = 200,
            RenderTransformOrigin = RelativePoint.Center,
            RenderTransform = new RotateTransform(rotationDegrees)
        };
        var layout = _layoutService.CalculateLayout(
            new Size(1000, 800),
            new Size(400, 200),
            1,
            rotationDegrees,
            default);

        Canvas.SetLeft(image, layout.CanvasPosition.X);
        Canvas.SetTop(image, layout.CanvasPosition.Y);
        canvas.Children.Add(image);
        window.Show();
        window.Measure(new Size(1000, 800));
        window.Arrange(new Rect(0, 0, 1000, 800));

        var corners = new[]
        {
            image.TranslatePoint(new Point(0, 0), canvas),
            image.TranslatePoint(new Point(image.Bounds.Width, 0), canvas),
            image.TranslatePoint(new Point(0, image.Bounds.Height), canvas),
            image.TranslatePoint(new Point(image.Bounds.Width, image.Bounds.Height), canvas)
        };

        Assert.DoesNotContain(corners, point => point is null);
        var transformedCorners = corners.Select(point => point!.Value).ToArray();
        var actualBounds = new Rect(
            transformedCorners.Min(point => point.X),
            transformedCorners.Min(point => point.Y),
            transformedCorners.Max(point => point.X) - transformedCorners.Min(point => point.X),
            transformedCorners.Max(point => point.Y) - transformedCorners.Min(point => point.Y));

        var expectedBounds = new Rect(expectedLeft, expectedTop, expectedRight - expectedLeft, expectedBottom - expectedTop);
        Assert.True(AreClose(expectedBounds, actualBounds), $"Expected {expectedBounds}, actual {actualBounds}.");
        window.Close();
    }

    private static bool AreClose(Rect expected, Rect actual)
    {
        const double tolerance = 0.001;
        return Math.Abs(expected.X - actual.X) < tolerance
            && Math.Abs(expected.Y - actual.Y) < tolerance
            && Math.Abs(expected.Width - actual.Width) < tolerance
            && Math.Abs(expected.Height - actual.Height) < tolerance;
    }
}
