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

/// <summary>
/// 为其他语言文本应用 ColorCode 语法高亮。
/// </summary>
public sealed class OtherCodeDocumentHighlighter : IDocumentHighlighter
{
    private readonly PlainTextDocumentHighlighter _plainTextDocumentHighlighter;
    private readonly TextEditor _textEditor;

    /// <summary>
    /// 创建其他语言文档高亮器。
    /// </summary>
    /// <param name="textEditor">要应用高亮的文本编辑器。</param>
    /// <param name="languageId">ColorCode 使用的语言标识。</param>
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

    /// <summary>
    /// 对指定文本应用高亮。
    /// </summary>
    /// <param name="text">要高亮的文本。</param>
    public void ApplyHighlight(string text)
    {
        _plainTextDocumentHighlighter.ApplyHighlight(text);
        var colorCode = new TextEditorColorCode(_textEditor, new DocumentOffset(0));
        var highlightCodeContext = new HighlightCodeContext(text, colorCode);
        _codeHighlighter.ApplyHighlight(highlightCodeContext);
    }

    /// <summary>
    /// 渲染背景层。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderBackground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderBackground(in context);
    }

    /// <summary>
    /// 渲染前景层。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderForeground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderForeground(in context);
    }
}
