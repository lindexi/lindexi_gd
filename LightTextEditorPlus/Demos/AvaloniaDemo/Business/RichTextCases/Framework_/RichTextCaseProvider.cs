using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;

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
        AddCommonCase();
        OnInit();
    }

    private void AddCommonCase()
    {
        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("123");
        }, "追加文本");

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
        }, "设置加粗");
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
