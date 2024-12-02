using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using SkiaSharp;

namespace LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

class RichTextCaseProvider
{
    public RichTextCaseProvider()
    {
        Add(editor =>
        {
            // 追加文本
            editor.AppendText("追加的文本");
        }, "追加文本");

        Add(editor =>
        {
            //editor.TextEditorCore.PlatformProvider.GetPlatformRunPropertyCreator()
            SkiaTextRunProperty runProperty = editor.GetDefaultRunProperty();
            runProperty = runProperty with
            {
                FontSize = 60
            };

            editor.AppendRun(new SkiaTextRun("文本", runProperty));
        }, "插入文本带大字号");

        Add(editor =>
        {

            SkiaTextRunProperty runProperty = editor.GetDefaultRunProperty();
            runProperty = runProperty with
            {
                FontSize = Random.Shared.Next(10,100),
                Foreground = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF),
            };
            editor.AppendRun(new SkiaTextRun("文本", runProperty));
        }, "随意的字符属性");
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
        RichTextCases[2].Exec(textEditor);
    }
}