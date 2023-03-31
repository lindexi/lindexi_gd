using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.TestsFramework;

public class TestPlatformProvider : PlatformProvider
{
    public override ICharInfoMeasurer? GetCharInfoMeasurer() => CharInfoMeasurer ?? base.GetCharInfoMeasurer();
    public ICharInfoMeasurer? CharInfoMeasurer { set; get; }

    public override IRenderManager? GetRenderManager() => RenderManager ?? base.GetRenderManager();

    public IRenderManager? RenderManager { set; get; }

    public IWholeLineLayouter? WholeLineLayouter { set; get; }

    public override IWholeLineLayouter? GetWholeRunLineLayouter()
    {
        return WholeLineLayouter ?? base.GetWholeRunLineLayouter();
    }

    public override ITextEditorUndoRedoProvider BuildTextEditorUndoRedoProvider()
    {
        return UndoRedoProvider ?? base.BuildTextEditorUndoRedoProvider();
    }

    public ITextEditorUndoRedoProvider? UndoRedoProvider { set; get; }

    public override ITextLogger? BuildTextLogger()
    {
        return TextLogger ?? base.BuildTextLogger();
    }

    public ITextLogger? TextLogger { set; get; }
}