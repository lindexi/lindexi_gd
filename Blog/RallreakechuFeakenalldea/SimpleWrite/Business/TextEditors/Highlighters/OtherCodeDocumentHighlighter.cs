using System;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

using SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;

namespace SimpleWrite.Business.TextEditors.Highlighters;

internal sealed class OtherCodeDocumentHighlighter : IDocumentHighlighter
{
    private readonly PlainTextDocumentHighlighter _plainTextDocumentHighlighter;
    private readonly SimpleWriteTextEditor _textEditor;

    public OtherCodeDocumentHighlighter(SimpleWriteTextEditor textEditor, string languageId)
    {
        ArgumentNullException.ThrowIfNull(textEditor);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageId);

        _textEditor = textEditor;
        _plainTextDocumentHighlighter = new PlainTextDocumentHighlighter(textEditor);
        _codeHighlighter = new ColorCodeCodeHighlighter
        {
            LanguageId = languageId
        };
    }

    private readonly ICodeHighlighter _codeHighlighter;

    public void ApplyHighlight(string text)
    {
        _plainTextDocumentHighlighter.ApplyHighlight(text);
        var colorCode = new TextEditorColorCode(_textEditor, new DocumentOffset(0));
        var highlightCodeContext = new HighlightCodeContext(text, colorCode);
        _codeHighlighter.ApplyHighlight(highlightCodeContext);
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
