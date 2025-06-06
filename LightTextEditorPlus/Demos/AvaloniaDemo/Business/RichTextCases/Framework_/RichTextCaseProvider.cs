using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus.Demo.Business.RichTextCases;

internal partial class RichTextCaseProvider
{
    public RichTextCaseProvider(Func<TextEditor> getTextEditor) : this(new DelegateTextEditorProvider(getTextEditor))
    {
    }

    public RichTextCaseProvider(ITextEditorProvider textEditorProvider)
    {
        _textEditorProvider = textEditorProvider;
        OnInit();
    }

    private void AddCommonCase()
    {
        // 文本和字符属性

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.Text = "Text";
        }, "直接设置文本内容");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("123");
        }, "追加文本");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            // 换行符用 \n 或 \r\n 都可以，文本库底层会自行处理
            textEditor.AppendText("123\nabc");
            textEditor.AppendText("def\r\n123");
        }, "追加两段文本");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.Text = "abc";
            textEditor.EditAndReplace("B", new Selection(new CaretOffset(1), 1));
        }, "替换文本内容");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("123");
            textEditor.SetFontSize(25);
        }, "设置字号");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("abc");
            // 设置光标选择范围为 0-1 的字符的字号。光标选择范围为 0-1 的字符就是 'a' 字符
            Selection selection = new Selection(new CaretOffset(0), 1);
            textEditor.SetFontSize(fontSize: 25, selection: selection);
        }, "设置给定范围的字符的字号");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("abc");
            Selection selection = new Selection(new CaretOffset(0), 2);
            textEditor.SetFontName("Times New Roman", selection);
        }, "设置字体");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("abc");
            Selection selection = new Selection(new CaretOffset(0), 2);
            textEditor.ToggleBold(selection);
        }, "开启或关闭加粗");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("abc");
            // 调用 GetAllDocumentSelection 可获取全选的选择范围，注： 这里只获取选择范围，不会将文本选中
            Selection selection = textEditor.GetAllDocumentSelection();
            textEditor.ToggleItalic(selection);
        }, "开启或关闭斜体");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("abc");
            Selection selection = textEditor.GetAllDocumentSelection();
            textEditor.ToggleUnderline(selection);
        }, "开启或关闭下划线");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("abc");
            Selection selection = textEditor.GetAllDocumentSelection();
            textEditor.ToggleStrikethrough(selection);
        }, "开启或关闭删除线");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("abc");
            Selection selection = textEditor.GetAllDocumentSelection();
            textEditor.ToggleEmphasisDots(selection);
        }, "开启或关闭着重号");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("x2");
            Selection selection = new Selection(new CaretOffset(1), 1);
            textEditor.ToggleSuperscript(selection);
        }, "开启或关闭上标");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("x2");
            Selection selection = new Selection(new CaretOffset(1), 1);
            textEditor.ToggleSubscript(selection);
        }, "开启或关闭下标");

        // 段落属性

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.Text = "Text";
            // 水平居中是段落属性的
            textEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Center
            });
        }, "设置文本水平居中");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.Text = "Text";
            // 水平居右是段落属性的
            textEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Right
            });
        }, "设置文本水平居右");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.Text = """
                              aaa
                              bbb
                              ccc
                              """;

            textEditor.ConfigParagraphProperty(new ParagraphIndex(1), property => property with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Center
            });
            textEditor.ConfigParagraphProperty(new ParagraphIndex(2), property => property with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Right
            });
        }, "设置指定段落属性");

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.Text = """
                              aaa
                              bbb
                              ccc
                              """;

            textEditor.ConfigParagraphProperty(new ParagraphIndex(2), property => property with
            {
                LineSpacing = new MultipleTextLineSpace(2)
            });
        }, "设置两倍行距");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            textEditor.Text = new string(Enumerable.Repeat('a', 100).ToArray());

            textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
            {
                Indent = 50,
                IndentType = IndentType.FirstLine,
            });
        }, "设置段落首行缩进");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            textEditor.Text = new string(Enumerable.Repeat('a', 100).ToArray());
            textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
            textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
            {
                Indent = 200,
                IndentType = IndentType.Hanging,
            });
        }, "设置段落悬挂缩进");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            // 制作两段，方便查看效果
            string text = new string(Enumerable.Repeat('a', 100).ToArray());
            textEditor.Text = text;
            textEditor.AppendText("\n" + text);

            textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
            textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
            {
                LeftIndentation = 100
            });
        }, "设置段落左侧缩进");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            // 制作两段，方便查看效果
            string text = new string(Enumerable.Repeat('a', 100).ToArray());
            textEditor.Text = text;
            textEditor.AppendText("\n" + text);

            textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
            textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
            {
                RightIndentation = 100
            });
        }, "设置段落右侧缩进");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            // 制作三段，方便查看效果
            string text = new string(Enumerable.Repeat('a', 100).ToArray());
            textEditor.Text = text;
            textEditor.AppendText("\n" + text + "\n" + text);

            textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
            textEditor.ConfigParagraphProperty(new ParagraphIndex(1), paragraphProperty => paragraphProperty with
            {
                ParagraphBefore = 100
            });
        }, "设置段前间距");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            // 制作三段，方便查看效果
            string text = new string(Enumerable.Repeat('a', 100).ToArray());
            textEditor.Text = text;
            textEditor.AppendText("\n" + text + "\n" + text);

            textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
            textEditor.ConfigParagraphProperty(new ParagraphIndex(1), paragraphProperty => paragraphProperty with
            {
                ParagraphAfter = 100
            });
        }, "设置段后间距");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            textEditor.AppendText("a\nb\nc");
            for (var i = 0; i < textEditor.ParagraphList.Count; i++)
            {
                textEditor.ConfigParagraphProperty(new ParagraphIndex(i), paragraphProperty => paragraphProperty with
                {
                    Marker = new BulletMarker()
                    {
                        MarkerText = "l",
                        RunProperty = textEditor.CreateRunProperty(runProperty => runProperty with
                        {
                            FontName = new FontName("Wingdings"),
                            FontSize = 15,
                        })
                    }
                });
            }
        }, "设置无序项目符号");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            textEditor.AppendText("a\nb\nc");
            var numberMarkerGroupId = new NumberMarkerGroupId();
            for (var i = 0; i < textEditor.ParagraphList.Count; i++)
            {
                textEditor.ConfigParagraphProperty(new ParagraphIndex(i), paragraphProperty =>
                {
                    return paragraphProperty with
                    {
                        Marker = new NumberMarker()
                        {
                            GroupId = numberMarkerGroupId
                        }
                    };
                });
            }
        }, "设置有序项目符号");
    }

    private partial void OnInit();

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
