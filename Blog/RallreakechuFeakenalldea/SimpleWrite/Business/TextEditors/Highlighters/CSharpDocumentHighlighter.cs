using LightTextEditorPlus;

namespace SimpleWrite.Business.TextEditors.Highlighters;

internal sealed class CSharpDocumentHighlighter : IDocumentHighlighter
{
    private readonly PlainTextDocumentHighlighter _plainTextDocumentHighlighter;

    public CSharpDocumentHighlighter(SimpleWriteTextEditor textEditor)
    {
        _plainTextDocumentHighlighter = new PlainTextDocumentHighlighter(textEditor);
    }

    public void ApplyHighlight(string text)
    {
        _plainTextDocumentHighlighter.ApplyHighlight(text);
    }

    public void RenderBackground(in AvaloniaTextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderBackground(in context);
    }

    public void RenderForeground(in AvaloniaTextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderForeground(in context);
    }
}
