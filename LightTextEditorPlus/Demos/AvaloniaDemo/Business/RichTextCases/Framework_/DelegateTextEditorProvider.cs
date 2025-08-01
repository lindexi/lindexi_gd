using System;

namespace LightTextEditorPlus.Demo.Business.RichTextCases;

class DelegateTextEditorProvider : ITextEditorProvider
{
    public DelegateTextEditorProvider(Func<TextEditor> getTextEditor)
    {
        _getTextEditor = getTextEditor;
    }

    private readonly Func<TextEditor> _getTextEditor;

    public TextEditor GetTextEditor()
    {
        return _getTextEditor();
    }
}
