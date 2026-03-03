using Avalonia.Skia;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Primitive;
using SkiaSharp;

namespace SimpleWrite.Business.TextEditors;

internal sealed class PlainTextDocumentHighlighter : IDocumentHighlighter
{
    private readonly SimpleWriteTextEditor _textEditor;
    private readonly SkiaTextRunProperty _normalTextRunProperty;

    public PlainTextDocumentHighlighter(SimpleWriteTextEditor textEditor)
    {
        _textEditor = textEditor;
        _normalTextRunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = 20,
            Foreground = new LightTextEditorPlus.Primitive.SolidColorSkiaTextBrush(SKColors.Azure)
        });
    }

    public void ApplyHighlight(string text)
    {
        _textEditor.SetRunProperty(_normalTextRunProperty, _textEditor.TextEditorCore.GetAllDocumentSelection());
    }

    public void Render(in AvaloniaTextEditorDrawingContext context)
    {
    }
}
