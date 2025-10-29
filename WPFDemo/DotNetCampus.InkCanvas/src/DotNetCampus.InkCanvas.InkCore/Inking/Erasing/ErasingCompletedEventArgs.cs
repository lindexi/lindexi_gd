using WejallkachawDadeawejearhuce.Inking.Erasing;

namespace WejallkachawDadeawejearhuce.Inking.Contexts;

public class ErasingCompletedEventArgs : EventArgs
{
    public ErasingCompletedEventArgs(IReadOnlyList<ErasingSkiaStroke> erasingSkiaStrokeList)
    {
        ErasingSkiaStrokeList = erasingSkiaStrokeList;
    }

    public IReadOnlyList<ErasingSkiaStroke> ErasingSkiaStrokeList { get; }
}