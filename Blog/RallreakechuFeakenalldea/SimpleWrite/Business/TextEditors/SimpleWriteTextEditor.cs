using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Media;
using Avalonia.Skia;

using LightTextEditorPlus;
using LightTextEditorPlus.Configurations;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Events;
using LightTextEditorPlus.Primitive;

using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Business.Snippets;
using SimpleWrite.Business.TextEditors.Highlighters;

using SkiaSharp;

using System;
using System.Threading.Tasks;

namespace SimpleWrite.Business.TextEditors;

/// <summary>
/// 文本编辑器
/// </summary>
internal sealed class SimpleWriteTextEditor : TextEditor
{
    public SimpleWriteTextEditor()
    {
        CaretConfiguration.SelectionBrush = new Color(0x9F, 0x26, 0x3F, 0xC7);
        CaretConfiguration.ShowCaretAndSelectionInReadonlyMode = true;

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
                var selectedText = GetText(in selection);

                Add("发送选中内容到 Copilot 聊天", () =>
                {
                    RaiseRequestSendTextToCopilot(selectedText);
                });

                Add("翻译为计算机英文", () =>
                {
                    var prompt =
                        $"""
                         请帮我将以下内容转述为地道的计算机英文，我将在即时聊天中使用：
                         {selectedText}
                         """;
                    RaiseRequestSendTextToCopilot(prompt);
                });

                Add("Json转C#类", () =>
                {
                    var prompt =
                        $"""
                         将以下 json 转换为 C# 的类型，要求使用 System.Text.Json 作为 Json 特性定义。要求 C# 属性命名符合 .NET 规范，采用帕斯卡风格：
                         {selectedText}
                         """;
                    RaiseRequestSendTextToCopilot(prompt);
                });

                void Add(string header, Action action)
                {
                    var menuItem = new MenuItem()
                    {
                        Header = header
                    };
                    menuItems.Add(menuItem);
                    menuItem.Click += (sender, eventArgs) => action();
                }
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
    private void RaiseRequestSendTextToCopilot(string text)
    {
        RequestSendTextToCopilot?.Invoke(this, text);
    }

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