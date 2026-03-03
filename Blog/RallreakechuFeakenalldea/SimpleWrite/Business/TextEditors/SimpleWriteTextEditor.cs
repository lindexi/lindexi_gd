using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;

using LightTextEditorPlus;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Primitive;

using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Business.Snippets;

using SkiaSharp;

using System;

namespace SimpleWrite.Business.TextEditors;

/// <summary>
/// 文本编辑器
/// </summary>
internal sealed class SimpleWriteTextEditor : TextEditor
{
    private IDocumentHighlighter _documentHighlighter;

    public SimpleWriteTextEditor()
    {
        CaretConfiguration.SelectionBrush = new Color(0x9F, 0x26, 0x3F, 0xC7);

        TextEditorCore.TextChanged += TextEditorCore_TextChanged;

        SizeToContent = SizeToContent.Height;

        var normalFontSize = 20;

        SetStyleTextRunProperty(runProperty => runProperty with
        {
            FontSize = normalFontSize,
            Foreground = new LightTextEditorPlus.Primitive.SolidColorSkiaTextBrush(SKColors.Azure)
        });

        _documentHighlighter = new MarkdownDocumentHighlighter(this);
    }

    public void SetDocumentHighlighter(IDocumentHighlighter documentHighlighter)
    {
        ArgumentNullException.ThrowIfNull(documentHighlighter);

        _documentHighlighter = documentHighlighter;
        ApplyHighlight();
    }

    private void TextEditorCore_TextChanged(object? sender, EventArgs e)
    {
        ApplyHighlight();
    }

    private void ApplyHighlight()
    {
        _documentHighlighter.ApplyHighlight(Text);
        InvalidateVisual();
    }

    /// <summary>
    /// 快捷键执行器
    /// </summary>
    public required ShortcutExecutor ShortcutExecutor { get; init; }

    /// <summary>
    /// 代码片管理器
    /// </summary>
    public required SnippetManager SnippetManager { get; init; }

    protected override TextEditorHandler CreateTextEditorHandler()
    {
        return new SimpleWriteTextEditorHandler(this);
    }

    protected override void Render(in AvaloniaTextEditorDrawingContext context)
    {
        _documentHighlighter.Render(in context);

        base.Render(in context);
    }
}