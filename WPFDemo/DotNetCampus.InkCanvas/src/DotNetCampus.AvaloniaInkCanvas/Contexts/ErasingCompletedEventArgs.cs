using DotNetCampus.Inking.Erasing;

namespace DotNetCampus.Inking.Contexts;

public class ErasingCompletedEventArgs : EventArgs
{
    public ErasingCompletedEventArgs(IReadOnlyList<ErasingSkiaStroke> erasingSkiaStrokeList)
    {
        ErasingSkiaStrokeList = erasingSkiaStrokeList;
    }

    public IReadOnlyList<ErasingSkiaStroke> ErasingSkiaStrokeList { get; }
}