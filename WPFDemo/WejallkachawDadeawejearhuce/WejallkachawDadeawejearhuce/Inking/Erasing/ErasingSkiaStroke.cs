namespace WejallkachawDadeawejearhuce.Inking.Erasing;

public readonly record struct ErasingSkiaStroke(SkiaStroke OriginStroke, IList<SkiaStroke> NewStrokeList);