using System;

using ColorCode.Common;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

using LightTextEditorPlus.Highlighters.CodeHighlighters;

#if USE_AVALONIA
using TextEditorDrawingContext = LightTextEditorPlus.AvaloniaTextEditorDrawingContext;
#elif USE_WPF
using TextEditorDrawingContext = LightTextEditorPlus.WpfTextEditorDrawingContext;
#endif

namespace LightTextEditorPlus.Highlighters;

public sealed class OtherCodeDocumentHighlighter : IDocumentHighlighter
{
    private readonly PlainTextDocumentHighlighter _plainTextDocumentHighlighter;
    private readonly TextEditor _textEditor;

    public OtherCodeDocumentHighlighter(TextEditor textEditor, string languageId)
    {
        ArgumentNullException.ThrowIfNull(textEditor);
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException($"{nameof(languageId)} cannot be null or whitespace.", nameof(languageId));
        }

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

    public void RenderBackground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderBackground(in context);
    }

    public void RenderForeground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderForeground(in context);
    }
}
