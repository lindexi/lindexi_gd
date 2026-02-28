using Avalonia.Input;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Editing;

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
                //snippetManager.Execute(TextEditor, snippet);
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

    protected override void OnPaste()
    {
        base.OnPaste();
    }
}