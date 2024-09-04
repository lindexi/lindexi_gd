namespace NarjejerechowainoBuwurjofear.Inking.Erasing;

record PointPathEraserResult(IReadOnlyList<ErasingSkiaStroke> ErasingSkiaStrokeList);

readonly record struct ErasingSkiaStroke(SkiaStroke OriginStroke, IList<SkiaStroke> NewStrokeList);