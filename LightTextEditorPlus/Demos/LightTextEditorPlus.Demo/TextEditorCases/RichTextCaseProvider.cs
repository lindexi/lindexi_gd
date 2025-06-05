using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;

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
            textEditor.AppendRun(new ImmutableRun("abc", textEditor.CreateRunProperty(property => property with
            {
                FontSize = 90,
                FontName = new FontName("Times New Roman"),
                FontWeight = FontWeights.Bold,
                DecorationCollection = TextEditorDecorations.Strikethrough
            })));
        }, "追加带格式的文本");
    }
}
