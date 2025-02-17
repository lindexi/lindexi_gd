using System;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.TestsFramework;

public class TestPlatformProvider : PlatformProvider
{
    public IPlatformRunPropertyCreator? FakePlatformRunPropertyCreator { get; set; }

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        return FakePlatformRunPropertyCreator ?? base.GetPlatformRunPropertyCreator();
    }

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

    public override ILineSpacingCalculator? GetLineSpacingCalculator()
    {
        return LineSpacingCalculator ?? base.GetLineSpacingCalculator();
    }

    public ILineSpacingCalculator? LineSpacingCalculator { set; get; }


    public HandleRequireDispatchUpdateLayout? RequireDispatchUpdateLayoutHandler { set; get; }

    public delegate void HandleRequireDispatchUpdateLayout(Action updateLayoutAction);

    public override void RequireDispatchUpdateLayout(Action updateLayoutAction)
    {
        if (RequireDispatchUpdateLayoutHandler != null)
        {
            RequireDispatchUpdateLayoutHandler(updateLayoutAction);
            return;
        }

        base.RequireDispatchUpdateLayout(updateLayoutAction);
    }

    public HandleInvokeDispatchUpdateLayout? InvokeDispatchUpdateLayoutHandler { set; get; }

    public delegate void HandleInvokeDispatchUpdateLayout(Action updateLayoutAction);

    public override void InvokeDispatchUpdateLayout(Action updateLayoutAction)
    {
        if (InvokeDispatchUpdateLayoutHandler != null)
        {
            InvokeDispatchUpdateLayoutHandler(updateLayoutAction);
        }
        else
        {
            base.InvokeDispatchUpdateLayout(updateLayoutAction);
        }
    }
}
