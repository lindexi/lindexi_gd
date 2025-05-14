using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
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

        var firstParagraphIndex = new ParagraphIndex(0);

        Add(editor =>
        {
            editor.SetFontSize(96);

            ParagraphProperty paragraphProperty = editor.GetParagraphProperty(firstParagraphIndex);
            editor.SetParagraphProperty(firstParagraphIndex, paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(2),
            });

            editor.SetFontName("微软雅黑");
            editor.AppendText("1asgf微软\r\n1gf雅黑");
        }, "测试行距");

        Add(editor =>
        {
            editor.SetFontSize(96);

            editor.TextEditorCore.LineSpacingStrategy = LineSpacingStrategy.FirstLineShrink;

            ParagraphProperty paragraphProperty = editor.GetParagraphProperty(firstParagraphIndex);
            editor.SetParagraphProperty(firstParagraphIndex, paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(2)
            });

            editor.SetFontName("微软雅黑");
            editor.AppendText("1asgf微软\r\n1asgf微软");
        }, "测试行距 首行不展开");

        Add(editor =>
        {
            editor.SetFontSize(50);
            editor.SetFontName("微软雅黑");

            editor.SetParagraphProperty(firstParagraphIndex, editor.StyleParagraphProperty with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Center
            });
            // 第一段长，第二段短
            editor.AppendText("asxfgi123雅黑\r\n短\r\n长长长");
        }, "宽度自适应的居中对齐");

        Add(editor =>
        {
            Append("1", 50);
            Append("f", 30);
            Append("g", 30);
            Append("g", 35);
            Append("g", 20);
            Append("g", 10);
            Append("g", 50);
            Append("f", 50);
            Append("f", 20);
            Append("中", 20);

            void Append(string text, double fontSize)
            {
                editor.AppendRun(new SkiaTextRun(text, editor.StyleRunProperty with
                {
                    FontSize = fontSize
                }));
            }
        }, "一行包含不同的字号的文本");

        Add(editor =>
        {
            editor.TextEditorCore.LineSpacingAlgorithm = LineSpacingAlgorithm.PPT;

            editor.SetParagraphProperty(firstParagraphIndex, editor.StyleParagraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(2),
            });

            editor.SetFontSize(92); // 92pixel = 69pound
            editor.AppendText("11asgf微软1gf雅黑");
        }, "行距测试-PPT行距算法-一段多行两倍行距");

        Add(editor =>
        {
            editor.TextEditorCore.LineSpacingAlgorithm = LineSpacingAlgorithm.PPT;

            editor.SetParagraphProperty(firstParagraphIndex, editor.StyleParagraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(3),
            });

            editor.SetFontSize(36); // 36pixel = 27pound
            editor.AppendText("1asgf微软\r\n1gf雅黑\r\n1gf中文");
        }, "行距测试-PPT行距算法-三段三倍行距");

        Add(editor =>
        {
            editor.TextEditorCore.LineSpacingAlgorithm = LineSpacingAlgorithm.WPF;

            editor.SetParagraphProperty(firstParagraphIndex, editor.StyleParagraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(14),
            });

            editor.SetFontSize(36); // 36pixel = 27pound
            editor.AppendText("1asgf微软\r\n1gf雅黑\r\n1gf中文");
        }, "行距测试-WPF行距算法-三段14倍行距");

        Add(editor =>
        {
            editor.TextEditorCore.LineSpacingAlgorithm = LineSpacingAlgorithm.WPF;
            editor.SetFontSize(36); // 36pixel = 27pound
            editor.AppendText("1asgf微软\r\n1gf雅黑\r\n1gf中文");

            ParagraphProperty paragraphProperty = editor.StyleParagraphProperty;
            editor.SetParagraphProperty(new ParagraphIndex(0), paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(4),
            });
            editor.SetParagraphProperty(new ParagraphIndex(1), paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(10),
            });
            editor.SetParagraphProperty(new ParagraphIndex(2), paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(14),
            });
        }, "行距测试-WPF行距算法-三段不同行距");

        Add(editor =>
        {
            editor.UseWordLineSpacingStrategy();
            editor.SetFontSize(36); // 36pixel = 27pound
            editor.AppendText("1asgf微软\r\n1gf雅黑\r\n1gf中文");

            ParagraphProperty paragraphProperty = editor.StyleParagraphProperty;
            editor.SetParagraphProperty(new ParagraphIndex(0), paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(4),
            });
            editor.SetParagraphProperty(new ParagraphIndex(1), paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(10),
            });
            editor.SetParagraphProperty(new ParagraphIndex(2), paragraphProperty with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(14),
            });
        }, "行距测试-Wod行距样式-三段不同行距");

        Add(editor =>
        {
            editor.SetFontSize(50);
            editor.SetFontName("微软雅黑");
            editor.AppendText("123123123123123123123123123");
            editor.SelectAll();
        }, "两行文本进行选择，选择范围不重叠");

        Add(editor =>
        {
            editor.SetFontSize(50);
            editor.SetFontName("微软雅黑");
            editor.CaretConfiguration.SelectionBrush = new Color(0x5C, 0xFF, 0x00, 0x00);
            editor.AppendText("123123123123123123123123123");
            editor.SelectAll();
        }, "设置选择范围颜色");

        Add(editor =>
        {
            editor.SetFontSize(50);
            editor.SetFontName("微软雅黑");
            editor.CaretConfiguration.SelectionBrush = new Color(0x5C, 0xFF, 0x00, 0x00);
            editor.AppendText("123123123123123123123123123");
            editor.SelectAll();
            editor.CaretConfiguration.ShowSelectionWhenNotInEditingInputMode = false;
        }, "设置失去焦点时，不要显示选择范围");

        Add(editor =>
        {
            TextElement.SetForeground(editor, Brushes.Coral);
            editor.AppendText("123123123123123123123123123");
        }, "设置前景色时，可设置到文本前景色");

        //Add(editor =>
        //{
        //    for (int i = 0; i < 10; i++)
        //    {
        //        for (int j = 0; j < 1000; j++)
        //        {
        //            editor.AppendText("123123123123123123123123123");
        //        }
        //        editor.AppendText("\r\n");
        //    }
        //},"测试超长的内容");

        //Add(editor =>
        //{
        //    if (editor.Parent is Panel panel)
        //    {
        //        panel.Children.RemoveAll(panel.Children.OfType<TextEditor>().ToList());

        //        TextEditor textEditor = new TextEditor();

        //        textEditor.AppendRun(new SkiaTextRun("1", textEditor.StyleRunProperty with
        //        {
        //            Foreground = SKColors.Red
        //        }));
        //        textEditor.AppendRun(new SkiaTextRun("2", textEditor.StyleRunProperty with
        //        {
        //            Foreground = SKColors.Blue
        //        }));

        //        textEditor.AppendRun(new SkiaTextRun("3"));

        //        panel.Children.Insert(0, textEditor);
        //    }
        //},"文本加入界面之前被设置颜色，颜色不会在加入界面之后被覆盖");

        Add(editor =>
        {
            editor.UseWpfLineSpacingStyle();
            editor.SetFontSize(30);
            editor.AppendText("一二一个中文雅黑对齐ddddddddd");
            editor.ArrangingType = ArrangingType.Vertical;
        }, "竖排文本");

        Add(editor =>
        {
            editor.UseWpfLineSpacingStyle();
            editor.SetFontSize(30);
            editor.AppendText("qpih12345609微软雅黑");
            editor.ArrangingType = ArrangingType.Vertical;
        }, "竖排文本多行");

        Add(editor =>
        {
            editor.AppendText("😊");
        }, "追加 Emoji 表情字符");
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
        //Run("一行包含不同的字号的文本");
        Run(_richTextCases.Last());
    }
}
