using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

public class TestPlatformProvider : PlatformProvider
{
    public IWholeLineLayouter? WholeLineLayouter { set; get; }

    public override IWholeLineLayouter? GetWholeRunLineLayouter()
    {
        return WholeLineLayouter ?? base.GetWholeRunLineLayouter();
    }
}