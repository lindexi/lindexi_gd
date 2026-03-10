using LightTextEditorPlus;

namespace SimpleWrite.Business.TextEditors.Highlighters;

internal interface IDocumentHighlighter
{
    void ApplyHighlight(string text);

    void RenderBackground(in AvaloniaTextEditorDrawingContext context);
    void RenderForeground(in AvaloniaTextEditorDrawingContext context);
}