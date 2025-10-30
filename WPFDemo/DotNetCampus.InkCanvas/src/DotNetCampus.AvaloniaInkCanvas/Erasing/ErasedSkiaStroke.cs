namespace DotNetCampus.Inking.Erasing;

public readonly record struct ErasedSkiaStroke(SkiaStroke OriginStroke, IList<SkiaStroke> NewStrokeList);