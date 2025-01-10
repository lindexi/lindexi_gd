using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using SkiaSharp;

namespace LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

class RichTextCaseProvider
{
    public RichTextCaseProvider(Func<TextEditor> getTextEditor) : this(new DelegateTextEditorProvider(getTextEditor))
    {

    }

    public RichTextCaseProvider(ITextEditorProvider textEditorProvider)
    {
        _textEditorProvider = textEditorProvider;
        Add(editor =>
        {
            // 追加文本
            editor.AppendText("追加的文本");
        }, "追加文本");

        Add(editor =>
        {
            editor.SetFontSize(76); // 76pixel = 57pound
            editor.SetFontName("微软雅黑");
            editor.AppendText("第一段afg\r\n第二段afg");
        }, "两段文本");

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
                FontSize = Random.Shared.Next(10, 100),
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
            // 这是一个裁剪过的字体，只包含有限的字符
            editor.SetFontName("仓耳小丸子");
            // 输入一个没有包含在字体中的字符
            editor.AppendText("一");
            // 预期能够自动回滚字体
        }, "测试字体回滚");

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

        Add(editor =>
        {
            editor.SetFontSize(96);

            ParagraphProperty paragraphProperty = editor.TextEditorCore.DocumentManager.GetParagraphProperty(0);
            editor.TextEditorCore.DocumentManager.SetParagraphProperty(0, paragraphProperty with
            {
                LineSpacing = 2
            });

            editor.SetFontName("微软雅黑");
            editor.AppendText("1asgf微软\r\n1gf雅黑");
        }, "测试行距");

        Add(editor =>
        {
            editor.SetFontSize(96);

            editor.TextEditorCore.LineSpacingStrategy = LineSpacingStrategy.FirstLineShrink;

            ParagraphProperty paragraphProperty = editor.TextEditorCore.DocumentManager.GetParagraphProperty(0);
            editor.TextEditorCore.DocumentManager.SetParagraphProperty(0, paragraphProperty with
            {
                LineSpacing = 2
            });

            editor.SetFontName("微软雅黑");
            editor.AppendText("1asgf微软\r\n1asgf微软");
        }, "测试行距 首行不展开");

        Add(editor =>
        {
            editor.SetFontSize(50);
            editor.SetFontName("微软雅黑");

            TextEditorCore textEditorCore = editor.TextEditorCore;
            textEditorCore.DocumentManager.SetParagraphProperty(0, textEditorCore.DocumentManager.DefaultParagraphProperty with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Center
            });
            // 第一段长，第二段短
            editor.AppendText("asxfgi123雅黑\r\n短\r\n长长长");
        }, "宽度自适应的居中对齐");
    }

    private readonly ITextEditorProvider _textEditorProvider;

    public TextEditor TextEditor => _textEditorProvider.GetTextEditor();

    public void Add(Action<TextEditor> action, string name = "")
    {
        Add(new DelegateRichTextCase(action, name));
    }

    public void Add(IRichTextCase richTextCase)
    {
        _richTextCases.Add(richTextCase);
    }

    public void Run(string richTextCaseName)
    {
        Run(this[richTextCaseName]);
    }

    public void Run(IRichTextCase richTextCase)
    {
        richTextCase.Exec(TextEditor);
        CurrentRichTextCase = richTextCase;
    }

    public IRichTextCase? CurrentRichTextCase { get; private set; }

    public IReadOnlyList<IRichTextCase> RichTextCases => _richTextCases;

    public IRichTextCase this[string name] => _richTextCases.Find(x => x.Name == name) ?? throw new Exception($"找不到 {name} 用例");

    private readonly List<IRichTextCase> _richTextCases = new List<IRichTextCase>();

    public void Debug()
    {
        //RichTextCases[2].Exec(textEditor);
        //Run("宽度自适应的居中对齐");
        Run("两段文本");
    }
}
