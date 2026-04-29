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

/// <summary>
/// 为 C# 文本应用语法高亮。
/// </summary>
public sealed class CSharpDocumentHighlighter : IDocumentHighlighter
{
    private readonly PlainTextDocumentHighlighter _plainTextDocumentHighlighter;
    private readonly TextEditor _textEditor;
    private readonly CsharpCodeHighlighter _csharpCodeHighlighter = new();

    /// <summary>
    /// 创建 C# 文档高亮器。
    /// </summary>
    /// <param name="textEditor">要应用高亮的文本编辑器。</param>
    public CSharpDocumentHighlighter(TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(textEditor);

        _textEditor = textEditor;
        _plainTextDocumentHighlighter = new PlainTextDocumentHighlighter(textEditor);
    }

    /// <summary>
    /// 对指定 C# 文本应用高亮。
    /// </summary>
    /// <param name="text">要高亮的 C# 文本。</param>
    public void ApplyHighlight(string text)
    {
        _plainTextDocumentHighlighter.ApplyHighlight(text);

        var colorCode = new TextEditorColorCode(_textEditor, new DocumentOffset(0));
        var highlightCodeContext = new HighlightCodeContext(text, colorCode);
        _csharpCodeHighlighter.ApplyHighlight(highlightCodeContext);
    }

    /// <summary>
    /// 渲染 C# 文本背景。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderBackground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderBackground(in context);
    }

    /// <summary>
    /// 渲染 C# 文本前景。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderForeground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderForeground(in context);
    }
}
