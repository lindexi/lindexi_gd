using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus.Demo.Business.RichTextCases;

internal partial class RichTextCaseProvider
{
    private partial void OnInit()
    {
        // 字符属性
        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendText("123");
        }, "追加文本");
    }
}


