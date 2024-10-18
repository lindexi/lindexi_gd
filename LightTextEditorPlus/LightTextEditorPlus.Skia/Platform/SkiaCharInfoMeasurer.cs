using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Platform;

class SkiaCharInfoMeasurer : ICharInfoMeasurer
{
    public SkiaCharInfoMeasurer(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private SkiaTextEditor TextEditor { get; }

    public CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo)
    {
        return new CharInfoMeasureResult();
    }
}