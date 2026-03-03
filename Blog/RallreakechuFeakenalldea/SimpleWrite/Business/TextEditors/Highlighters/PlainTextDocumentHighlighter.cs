using Avalonia.Skia;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Primitive;

using SkiaSharp;

namespace SimpleWrite.Business.TextEditors.Highlighters;

internal sealed class PlainTextDocumentHighlighter : IDocumentHighlighter
{
    private readonly SimpleWriteTextEditor _textEditor;
    private readonly SkiaTextRunProperty _normalTextRunProperty;

    public PlainTextDocumentHighlighter(SimpleWriteTextEditor textEditor)
    {
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

    public void RenderBackground(in AvaloniaTextEditorDrawingContext context)
    {
    }

    public void RenderForeground(in AvaloniaTextEditorDrawingContext context)
    {
    }
}
