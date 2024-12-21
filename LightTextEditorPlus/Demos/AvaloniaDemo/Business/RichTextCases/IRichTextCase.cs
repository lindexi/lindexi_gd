using LightTextEditorPlus;

namespace LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

interface IRichTextCase
{
    string Name { get; }
    void Exec(TextEditor textEditor);
}
