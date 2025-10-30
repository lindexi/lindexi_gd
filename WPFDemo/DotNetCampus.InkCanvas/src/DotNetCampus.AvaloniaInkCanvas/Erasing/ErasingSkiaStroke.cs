namespace DotNetCampus.Inking.Erasing;

public readonly record struct ErasingSkiaStroke(SkiaStroke OriginStroke, IList<SkiaStroke> NewStrokeList);