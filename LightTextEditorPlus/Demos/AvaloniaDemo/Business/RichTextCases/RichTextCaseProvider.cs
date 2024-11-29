using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

class RichTextCaseProvider
{
    public RichTextCaseProvider()
    {
        Add(editor =>
        {
            // 追加文本
            editor.TextEditorCore.AppendText("追加的文本");
        }, "追加文本");
    }

    public void Add(Action<TextEditor> action, string name = "")
    {
        Add(new DelegateRichTextCase(action));
    }

    public void Add(IRichTextCase richTextCase)
    {
        _richTextCases.Add(richTextCase);
    }

    public IReadOnlyList<IRichTextCase> RichTextCases => _richTextCases;

    private readonly List<IRichTextCase> _richTextCases = new List<IRichTextCase>();

    public void Debug(TextEditor textEditor)
    {
        RichTextCases[0].Exec(textEditor);
    }
}