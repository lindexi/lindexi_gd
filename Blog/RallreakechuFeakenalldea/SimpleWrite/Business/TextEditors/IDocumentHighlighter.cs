using LightTextEditorPlus;

namespace SimpleWrite.Business.TextEditors;

internal interface IDocumentHighlighter
{
    void ApplyHighlight(string text);

    void Render(in AvaloniaTextEditorDrawingContext context);
}
