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

/// <summary>
/// 为纯文本应用默认样式。
/// </summary>
public sealed class PlainTextDocumentHighlighter : IDocumentHighlighter
{
    private readonly TextEditor _textEditor;
    private readonly RunProperty _normalTextRunProperty;

    /// <summary>
    /// 创建纯文本高亮器。
    /// </summary>
    /// <param name="textEditor">要应用高亮的文本编辑器。</param>
    public PlainTextDocumentHighlighter(TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(textEditor);

        _textEditor = textEditor;
        _normalTextRunProperty = textEditor.StyleRunProperty;
    }

    /// <summary>
    /// 将整个文档恢复为默认文本样式。
    /// </summary>
    /// <param name="text">当前文档文本。</param>
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

    /// <summary>
    /// 渲染纯文本背景。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderBackground(in TextEditorDrawingContext context)
    {
    }

    /// <summary>
    /// 渲染纯文本前景。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderForeground(in TextEditorDrawingContext context)
    {
    }
}
