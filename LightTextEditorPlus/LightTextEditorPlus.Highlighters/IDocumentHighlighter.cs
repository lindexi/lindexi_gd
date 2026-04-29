using LightTextEditorPlus;

namespace LightTextEditorPlus.Highlighters;

public interface IDocumentHighlighter
{
    void ApplyHighlight(string text);

    void RenderBackground(in AvaloniaTextEditorDrawingContext context);
    void RenderForeground(in AvaloniaTextEditorDrawingContext context);
}