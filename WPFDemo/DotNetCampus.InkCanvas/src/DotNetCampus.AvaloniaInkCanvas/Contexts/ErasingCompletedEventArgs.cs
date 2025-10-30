using NarjejerechowainoBuwurjofear.Inking.Erasing;

namespace NarjejerechowainoBuwurjofear.Inking.Contexts;

public class ErasingCompletedEventArgs : EventArgs
{
    public ErasingCompletedEventArgs(IReadOnlyList<ErasingSkiaStroke> erasingSkiaStrokeList)
    {
        ErasingSkiaStrokeList = erasingSkiaStrokeList;
    }

    public IReadOnlyList<ErasingSkiaStroke> ErasingSkiaStrokeList { get; }
}