using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Highlighters;

using SimpleWrite.Business.ShortcutManagers;

namespace SimpleWrite.Business.TextEditors;

class SimpleWriteTextEditorHandler : TextEditorHandler
{
    public SimpleWriteTextEditorHandler(SimpleWriteTextEditor textEditor) : base(textEditor)
    {
        SimpleWriteTextEditor = textEditor;
    }

    public SimpleWriteTextEditor SimpleWriteTextEditor { get; }

    private ShortcutExecutor ShortcutExecutor => SimpleWriteTextEditor.ShortcutExecutor;

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!e.Pointer.IsPrimary)
        {
            // 多指就不要捣乱
            return;
        }

        if (e.Handled)
        {
            // 预期不会进入此分支
            return;
        }

        if (e.KeyModifiers is KeyModifiers.Control && e.InitialPressMouseButton == MouseButton.Left)
        {
            // 采用 ctrl + 鼠标左键 来触发超链接点击
            // 判断命中测试是否在超链接上，如果是，就触发点击事件
            var position = e.GetPosition(TextEditor);
            TextPoint textPoint = new(position.X, position.Y);
            var hitResult = TryHitHyperlink(in textPoint);
            if (hitResult)
            {
                e.Handled = true;
            }
        }

        base.OnPointerReleased(e);
    }

    private bool TryHitHyperlink(in TextPoint textPoint)
    {
        if (SimpleWriteTextEditor.DocumentHighlighter is not MarkdownDocumentHighlighter markdownDocumentHighlighter)
        {
            return false;
        }

        if (!TextEditorCore.TryHitTest(in textPoint, out var result))
        {
            return false;
        }

        if (result.IsHitSpace)
        {
            return false;
        }

        foreach (var markdownUrlInfo in markdownDocumentHighlighter.UrlInfoList)
        {
            var sourceSpan = markdownUrlInfo.SourceSpan;
            var hitCaretOffset = result.HitCaretOffset.Offset;

            if(sourceSpan.Start <= hitCaretOffset
               && hitCaretOffset <= sourceSpan.End
               && markdownUrlInfo.Url is var url)
            {
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });

                return true;
            }
        }

        return false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // 判断是否落在快捷键范围内
        var shortcutHandled = ShortcutExecutor.Handle(e, new ShortcutExecuteContext
        {
            CurrentTextEditor = SimpleWriteTextEditor,
        });
        if (shortcutHandled)
        {
            // 被快捷键处理了，就不继续往下传递
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            e.Handled = true;

            ITextParagraph paragraph = TextEditor.GetCurrentCaretOffsetParagraph();
            TextReadOnlyListSpan<CharData> charDataList = paragraph.GetParagraphCharDataList();

            var snippetManager = SimpleWriteTextEditor.SnippetManager;
            var snippet = snippetManager.Match(charDataList);

            if (snippet != null)
            {
                var paragraphSelection = TextEditor.GetParagraphSelection(paragraph);
                TextEditor.EditAndReplace(snippet.ContentText, paragraphSelection);
                // 再设置光标
                TextEditor.CurrentCaretOffset =
                    new CaretOffset(paragraphSelection.StartOffset.Offset + snippet.RelativeCaretOffset);
            }

            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
    }

    protected override void OnCut()
    {
        var selection = TextEditor.CurrentSelection;
        if (selection.IsEmpty)
        {
            // 空的话，直接剪切段
            ITextParagraph paragraph = TextEditor.GetParagraph(selection.StartOffset);
            var paragraphSelection = TextEditor.GetParagraphSelection(paragraph);

            string text = TextEditor.GetText(in paragraphSelection);
            _ = GetClipboard()?.SetTextAsync(text);
            TextEditor.Remove(in paragraphSelection);
        }

        base.OnCut();
    }

    protected override void OnPaste()
    {
        base.OnPaste();
    }

    private IClipboard? GetClipboard()
    {
        if (TopLevel.GetTopLevel(TextEditor) is { } topLevel)
        {
            return topLevel.Clipboard;
        }

        return null;
    }
}