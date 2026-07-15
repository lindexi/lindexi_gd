using Avalonia;
using ImageViewer.Models;
using ImageViewer.Services;

namespace ImageViewer.Tests.Services;

public sealed class ImageViewerStateTests
{
    [Fact]
    public void ResetImageStateClearsLoadedImageAndRestoresDefaults()
    {
        var state = new ImageViewerState
        {
            ImagePaths = ["a.png", "b.png"],
            CurrentInfo = new ImageFileInfo("a.png", "a.png", 10, 100, 80, "PNG"),
            CurrentIndex = 1,
            FitZoom = 0.5,
            Zoom = 3,
            IsFitMode = false,
            RotationDegrees = 90,
            PanOffset = new Point(20, -15),
            LastPanPoint = new Point(1, 2)
        };

        state.ResetImageState();

        Assert.Null(state.CurrentInfo);
        Assert.Empty(state.ImagePaths);
        Assert.Equal(-1, state.CurrentIndex);
        Assert.Equal(1, state.FitZoom);
        Assert.Equal(1, state.Zoom);
        Assert.True(state.IsFitMode);
        Assert.Equal(0, state.RotationDegrees);
        Assert.Equal(default, state.PanOffset);
        Assert.Null(state.LastPanPoint);
    }

    [Fact]
    public void StateCanRepresentFitAndActualSizeToggle()
    {
        var state = new ImageViewerState
        {
            FitZoom = 0.25,
            Zoom = 0.25,
            IsFitMode = true
        };

        state.Zoom = 1;
        state.IsFitMode = false;

        Assert.Equal(1, state.Zoom);
        Assert.False(state.IsFitMode);
    }
}
