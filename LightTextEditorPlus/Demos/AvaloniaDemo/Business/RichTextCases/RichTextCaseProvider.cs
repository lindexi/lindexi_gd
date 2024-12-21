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
            SkiaTextRunProperty runProperty = editor.CurrentCaretRunProperty;
            runProperty = runProperty with
            {
                FontSize = 60
            };

            editor.AppendRun(new SkiaTextRun("文本", runProperty));
        }, "插入文本带大字号");

        Add(editor =>
        {

            SkiaTextRunProperty runProperty = editor.CurrentCaretRunProperty;
            runProperty = runProperty with
            {
                FontSize = Random.Shared.Next(10,100),
                Foreground = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF),
            };
            editor.AppendRun(new SkiaTextRun("文本", runProperty));
        }, "随意的字符属性");

        Add(editor =>
        {
            editor.SetFontName("仓耳小丸子");
            editor.AppendText("仓耳小丸子");
        }, "测试内嵌字体");

        Add(editor =>
        {
            editor.SetFontSize(50);
            editor.SetFontName("微软雅黑");

            editor.AppendText("asxfgijpqWER123雅黑");

        }, "测试四线三格");

        Add(editor =>
        {
            editor.SetFontName("华文仿宋");
            editor.SetFontSize(90);
            editor.AppendText("123asdfgg宋体ggggf");
            // 预期行高 = (1.2018 * 1 +  0.0034) * 90 = 108.468 pixel 
        }, "测试华文仿宋字体");
    }

    public void Add(Action<TextEditor> action, string name = "")
    {
        Add(new DelegateRichTextCase(action, name));
    }

    public void Add(IRichTextCase richTextCase)
    {
        _richTextCases.Add(richTextCase);
    }

    public IReadOnlyList<IRichTextCase> RichTextCases => _richTextCases;

    public IRichTextCase this[string name] => _richTextCases.Find(x => x.Name == name) ?? throw new Exception($"找不到 {name} 用例");

    private readonly List<IRichTextCase> _richTextCases = new List<IRichTextCase>();

    public void Debug(TextEditor textEditor)
    {
        //RichTextCases[2].Exec(textEditor);
        this["测试四线三格"].Exec(textEditor);
    }
}
