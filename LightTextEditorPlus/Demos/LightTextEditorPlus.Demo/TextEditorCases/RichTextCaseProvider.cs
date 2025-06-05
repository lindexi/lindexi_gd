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
    private partial void OnInit()
    {
        // 示例
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
            textEditor.SetFontName("Times New Roman");
        }, "设置字体");
    }
}
