using LightTextEditorPlus;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

using LightTextEditorPlus.Highlighters.CodeHighlighters;

using System;

#if USE_AVALONIA
using TextEditorDrawingContext = LightTextEditorPlus.AvaloniaTextEditorDrawingContext;
#elif USE_WPF
using TextEditorDrawingContext = LightTextEditorPlus.WpfTextEditorDrawingContext;
#endif

namespace LightTextEditorPlus.Highlighters;

public sealed class CSharpDocumentHighlighter : IDocumentHighlighter
{
    private readonly PlainTextDocumentHighlighter _plainTextDocumentHighlighter;
    private readonly TextEditor _textEditor;
    private readonly CsharpCodeHighlighter _csharpCodeHighlighter = new();

    public CSharpDocumentHighlighter(TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(textEditor);

        _textEditor = textEditor;
        _plainTextDocumentHighlighter = new PlainTextDocumentHighlighter(textEditor);
    }

    public void ApplyHighlight(string text)
    {
        _plainTextDocumentHighlighter.ApplyHighlight(text);

        var colorCode = new TextEditorColorCode(_textEditor, new DocumentOffset(0));
        var highlightCodeContext = new HighlightCodeContext(text, colorCode);
        _csharpCodeHighlighter.ApplyHighlight(highlightCodeContext);
    }

    public void RenderBackground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderBackground(in context);
    }

    public void RenderForeground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderForeground(in context);
    }
}
