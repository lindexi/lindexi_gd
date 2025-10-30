using System.Collections.Generic;
using DotNetCampus.Inking.Primitive;

namespace WpfInk;

public readonly record struct StrokeRendererInfo
{
    public required IReadOnlyList<InkStylusPoint> StylusPointCollection { get; init; }

    public required double Width { get; init; }
    public required double Height { get; init; }

    public bool FitToCurve { get; init; }
}