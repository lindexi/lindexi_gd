using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

public class TestPlatformProvider : PlatformProvider
{
    public override IRenderManager? GetRenderManager() => RenderManager ?? base.GetRenderManager();

    public IRenderManager? RenderManager { set; get; }


    public IWholeLineLayouter? WholeLineLayouter { set; get; }

    public override IWholeLineLayouter? GetWholeRunLineLayouter()
    {
        return WholeLineLayouter ?? base.GetWholeRunLineLayouter();
    }
}