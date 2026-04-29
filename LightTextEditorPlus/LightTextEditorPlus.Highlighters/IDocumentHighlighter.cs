using LightTextEditorPlus;

#if USE_AVALONIA
using TextEditorDrawingContext = LightTextEditorPlus.AvaloniaTextEditorDrawingContext;
#elif USE_WPF
using TextEditorDrawingContext = LightTextEditorPlus.WpfTextEditorDrawingContext;
#endif

namespace LightTextEditorPlus.Highlighters;

/// <summary>
/// 定义文档高亮器的基础能力。
/// </summary>
public interface IDocumentHighlighter
{
    /// <summary>
    /// 对指定文本应用高亮。
    /// </summary>
    /// <param name="text">要高亮的文本。</param>
    void ApplyHighlight(string text);

    /// <summary>
    /// 渲染背景层。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    void RenderBackground(in TextEditorDrawingContext context);

    /// <summary>
    /// 渲染前景层。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    void RenderForeground(in TextEditorDrawingContext context);
}