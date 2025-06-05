using System;

namespace LightTextEditorPlus.Demo.Business.RichTextCases;

class DelegateRichTextCase(Action<TextEditor> action, string name) : IRichTextCase
{
    public string Name => name;

    public void Exec(TextEditor textEditor)
    {
        action(textEditor);
    }
}
