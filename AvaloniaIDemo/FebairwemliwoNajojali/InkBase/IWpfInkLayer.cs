using NarjejerechowainoBuwurjofear.Inking.Contexts;

namespace InkBase;

public interface IWpfInkLayer
{
    StandardRgbColor Color { set; get; }
    double InkThickness { set; get; }

    void Down(InkPoint screenPoint);
    void Move(InkPoint screenPoint);
    void Up(InkPoint screenPoint);

    event EventHandler<SkiaStroke>? StrokeCollected;

    void HideStroke(SkiaStroke skiaStroke);

    SkiaStroke PointListToStroke(InkId id, IReadOnlyList<InkPoint> points);
}