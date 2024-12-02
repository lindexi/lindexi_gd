using System;

namespace LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

class DelegateRichTextCase(Action<TextEditor> action) : IRichTextCase
{
    public void Exec(TextEditor textEditor)
    {
        action(textEditor);
    }
}