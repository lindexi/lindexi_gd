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
using SimpleWrite.Business.TextEditors.CommandPatterns;

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

    public CommandPatternManager? CommandPatternManager { get; init; }

    protected override async void OnRaisePrepareContextMenuEvent(PrepareContextMenuEventArgs args)
    {
        try
        {
            base.OnRaisePrepareContextMenuEvent(args);

            if (ContextMenu?.Items is { } menuItems && CommandPatternManager != null)
            {
                var selection = CurrentSelection;
                string selectedText;
                bool isSingleLine;

                if (!selection.IsEmpty)
                {
                    selectedText = GetText(in selection);
                    isSingleLine = false;
                }
                else if (args.TryHitTest(out var result))
                {
                    isSingleLine = true;

                    var hitParagraphData = result.HitParagraphData;
                    if (hitParagraphData.IsEmptyParagraph)
                    {
                        // 空段落啥都不干
                        selectedText = string.Empty;
                    }
                    else
                    {
                        selectedText = hitParagraphData.GetText();
                    }
                }
                else
                {
                    isSingleLine = true;
                    selectedText = string.Empty;
                }

                if (!string.IsNullOrEmpty(selectedText))
                {
                    foreach (var commandPattern in CommandPatternManager.CommandPatternList)
                    {
                        if (isSingleLine && !commandPattern.SupportSingleLine)
                        {
                            continue;
                        }

                        if (await commandPattern.IsMatchAsync(selectedText))
                        {
                            var menuItem = new MenuItem()
                            {
                                Header = commandPattern.Title
                            };
                            menuItems.Add(menuItem);
                            menuItem.Click += (sender, eventArgs) => _ = commandPattern.DoAsync(selectedText);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            // 先忽略
        }
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