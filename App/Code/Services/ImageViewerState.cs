using System;
using System.Collections.Generic;
using Avalonia;
using ImageViewer.Models;

namespace ImageViewer.Services;

internal sealed class ImageViewerState
{
    public IReadOnlyList<string> ImagePaths { get; set; } = Array.Empty<string>();

    public ImageFileInfo? CurrentInfo { get; set; }

    public int CurrentIndex { get; set; } = -1;

    public double FitZoom { get; set; } = 1;

    public double Zoom { get; set; } = 1;

    public bool IsFitMode { get; set; } = true;

    public int RotationDegrees { get; set; }

    public Point PanOffset { get; set; }

    public Point? LastPanPoint { get; set; }

    public void ResetImageState()
    {
        CurrentInfo = null;
        ImagePaths = Array.Empty<string>();
        CurrentIndex = -1;
        FitZoom = 1;
        Zoom = 1;
        IsFitMode = true;
        RotationDegrees = 0;
        PanOffset = default;
        LastPanPoint = null;
    }
}
