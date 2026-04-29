using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;

using System;

#if USE_AVALONIA
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
using TextEditorDrawingContext = LightTextEditorPlus.AvaloniaTextEditorDrawingContext;
#elif USE_WPF
using RunProperty = LightTextEditorPlus.Document.RunProperty;
using TextEditorDrawingContext = LightTextEditorPlus.WpfTextEditorDrawingContext;
#endif

namespace LightTextEditorPlus.Highlighters;

public sealed class PlainTextDocumentHighlighter : IDocumentHighlighter
{
    private readonly TextEditor _textEditor;
    private readonly RunProperty _normalTextRunProperty;

    public PlainTextDocumentHighlighter(TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(textEditor);

        _textEditor = textEditor;
        _normalTextRunProperty = textEditor.StyleRunProperty;
    }

    public void ApplyHighlight(string text)
    {
        var allDocumentSelection = _textEditor.TextEditorCore.GetAllDocumentSelection();
        var areAllRunPropertiesMatch = _textEditor.TextEditorCore.DocumentManager.AreAllRunPropertiesMatch<IReadOnlyRunProperty>(runProperty=> _normalTextRunProperty.Equals(runProperty), allDocumentSelection);

        if (!areAllRunPropertiesMatch)
        {
            // 如果不是所有的文本都使用正常样式，则刷一下
            _textEditor.SetRunProperty(_normalTextRunProperty, allDocumentSelection);
        }
    }

    public void RenderBackground(in TextEditorDrawingContext context)
    {
    }

    public void RenderForeground(in TextEditorDrawingContext context)
    {
    }
}
