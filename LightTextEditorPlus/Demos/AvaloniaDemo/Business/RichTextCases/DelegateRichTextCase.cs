using System;
using LightTextEditorPlus;

namespace LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

class DelegateRichTextCase(Action<TextEditor> action, string name) : IRichTextCase
{
    public string Name => name;

    public void Exec(TextEditor textEditor)
    {
        action(textEditor);
    }
}
