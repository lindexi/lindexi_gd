using DotNetCampus.Inking.Erasing;

namespace DotNetCampus.Inking.Contexts;

public class ErasingCompletedEventArgs : EventArgs
{
    public ErasingCompletedEventArgs(IReadOnlyList<ErasedSkiaStroke> erasingSkiaStrokeList)
    {
        ErasingSkiaStrokeList = erasingSkiaStrokeList;
    }

    public IReadOnlyList<ErasedSkiaStroke> ErasingSkiaStrokeList { get; }
}