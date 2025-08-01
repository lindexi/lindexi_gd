using LightTextEditorPlus;

namespace LightTextEditorPlus.Demo.Business.RichTextCases;

interface IRichTextCase
{
    string Name { get; }
    void Exec(TextEditor textEditor);
}
