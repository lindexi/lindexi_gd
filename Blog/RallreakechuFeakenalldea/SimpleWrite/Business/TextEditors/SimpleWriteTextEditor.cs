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
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using LightTextEditorPlus.Events;

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

        DocumentHighlighter = new MarkdownDocumentHighlighter(this);

        ContextMenu = new ContextMenu();
        //ContextMenu.Items.Add(new MenuItem()
        //{
        //    Header = "复制"
        //});

        ContextMenu.Closed += (sender, args) =>
        {
            // 每次都清理，等待下一次再重新加入内容
            ContextMenu.Items.Clear();
        };
    }

    protected override void OnRaisePrepareContextMenuEvent(PrepareContextMenuEventArgs args)
    {
        if (ContextMenu?.Items is { } menuItems && RequestSendTextToCopilot != null)
        {
            var selection = CurrentSelection;

            if (!selection.IsEmpty)
            {
                var sendTextToCopilotMenuItem = new MenuItem()
                {
                    Header = "发送选中内容到 Copilot 聊天"
                };
                menuItems.Add(sendTextToCopilotMenuItem);
                sendTextToCopilotMenuItem.Click += SendTextToCopilotMenuItemOnClick;
            }
            else if (args.TryHitTest(out var result))
            {
                var hitParagraphData = result.HitParagraphData;
                if (hitParagraphData.IsEmptyParagraph)
                {
                    // 空段落啥都不干
                }
                else
                {
                    var sendTextToCopilotMenuItem = new MenuItem()
                    {
                        Header = "发送当前段落内容到 Copilot 聊天"
                    };
                    sendTextToCopilotMenuItem.Click += (sender, eventArgs) =>
                    {
                        var text = hitParagraphData.GetText();
                        RequestSendTextToCopilot?.Invoke(this, text);
                    };
                    menuItems.Add(sendTextToCopilotMenuItem);
                }
            }
        }

        base.OnRaisePrepareContextMenuEvent(args);
    }

    private void SendTextToCopilotMenuItemOnClick(object? sender, EventArgs e)
    {
        var selection = CurrentSelection;
        if (selection.IsEmpty)
        {
            return;
        }

        var selectedText = GetText(in selection);
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            return;
        }

        RequestSendTextToCopilot?.Invoke(this, selectedText);
    }

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
        DocumentHighlighter.ApplyHighlight(Text);
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

    public event EventHandler<string>? RequestSendTextToCopilot;

    public IDocumentHighlighter DocumentHighlighter { get; private set; }

    protected override TextEditorHandler CreateTextEditorHandler()
    {
        return new SimpleWriteTextEditorHandler(this);
    }

    protected override void Render(in AvaloniaTextEditorDrawingContext context)
    {
        DocumentHighlighter.RenderBackground(in context);

        base.Render(in context);

        DocumentHighlighter.RenderForeground(in context);
    }
}