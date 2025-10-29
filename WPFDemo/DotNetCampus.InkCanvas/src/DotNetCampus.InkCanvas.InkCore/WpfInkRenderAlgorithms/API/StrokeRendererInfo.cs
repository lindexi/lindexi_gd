using System.Collections.Generic;

namespace WpfInk;

public readonly record struct StrokeRendererInfo
{
    public required IReadOnlyList<InkStylusPoint2D> StylusPointCollection { get; init; }

    public required double Width { get; init; }
    public required double Height { get; init; }

    public bool FitToCurve { get; init; }
}