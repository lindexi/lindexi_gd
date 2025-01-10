using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

public class SkiaTextRun : TextRun
{
    public SkiaTextRun(string text, SkiaTextRunProperty? runProperty = null) : base(text, runProperty)
    {
    }
}
