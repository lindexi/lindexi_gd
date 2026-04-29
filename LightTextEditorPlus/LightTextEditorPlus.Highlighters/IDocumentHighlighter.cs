using LightTextEditorPlus;

#if USE_AVALONIA
using TextEditorDrawingContext = LightTextEditorPlus.AvaloniaTextEditorDrawingContext;
#elif USE_WPF
using TextEditorDrawingContext = LightTextEditorPlus.WpfTextEditorDrawingContext;
#endif

namespace LightTextEditorPlus.Highlighters;

public interface IDocumentHighlighter
{
    void ApplyHighlight(string text);

    void RenderBackground(in TextEditorDrawingContext context);
    void RenderForeground(in TextEditorDrawingContext context);
}