using LightTextEditorPlus;

namespace LightTextEditorPlus.Demo.Business.RichTextCases;

public interface IRichTextCase
{
    string Name { get; }
    void Exec(TextEditor textEditor);
}
