using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;

using LightTextEditorPlus;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Primitive;

using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Business.Snippets;
using SimpleWrite.Business.TextEditors.Highlighters;

using SkiaSharp;

using System;
using Avalonia.Controls.Platform;

namespace SimpleWrite.Business.TextEditors;

/// <summary>
/// 文本编辑器
/// </summary>
internal sealed class SimpleWriteTextEditor : TextEditor
{
    public SimpleWriteTextEditor()
    {
        CaretConfiguration.SelectionBrush = new Color(0x9F, 0x26, 0x3F, 0xC7);

        TextEditorCore.TextChanged += TextEditorCore_TextChanged;

        SizeToContent = SizeToContent.Height;

        var normalFontSize = 20;

        SetStyleTextRunProperty(runProperty => runProperty with
        {
            FontSize = normalFontSize,
            Foreground = new SolidColorSkiaTextBrush(SKColors.Azure)
        });

        _documentHighlighter = new MarkdownDocumentHighlighter(this);

        ContextMenu = new ContextMenu();
        ContextMenu.Items.Add(new MenuItem()
        {
            Header = "复制"
        });
        ContextMenu.Opening += (sender, args) =>
        {

        };
    }

    private IDocumentHighlighter _documentHighlighter;

    //public void SetDocumentHighlighter(IDocumentHighlighter documentHighlighter)
    //{
    //    ArgumentNullException.ThrowIfNull(documentHighlighter);

    //    _documentHighlighter = documentHighlighter;
    //    ApplyHighlight();
    //}

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
        _documentHighlighter.RenderBackground(in context);

        base.Render(in context);

        _documentHighlighter.RenderForeground(in context);
    }
}