using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;
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
