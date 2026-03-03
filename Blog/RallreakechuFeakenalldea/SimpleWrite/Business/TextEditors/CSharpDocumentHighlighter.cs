using LightTextEditorPlus;

namespace SimpleWrite.Business.TextEditors;

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

    public void Render(in AvaloniaTextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.Render(in context);
    }
}
