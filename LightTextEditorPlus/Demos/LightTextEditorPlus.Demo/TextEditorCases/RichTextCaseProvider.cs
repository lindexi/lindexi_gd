using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus.Demo.Business.RichTextCases;

internal partial class RichTextCaseProvider
{
    private partial void OnInit()
    {
        AddCommonCase();

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

        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.Text = "abc";
            // 选中 'b' 这个字符
            Selection selection = new Selection(new CaretOffset(1), 1);
            RunProperty newRunProperty = textEditor.CreateRunProperty(property => property with
            {
                FontSize = 90,
                Foreground = new ImmutableBrush(Brushes.Red)
            });
            textEditor.EditAndReplaceRun(new ImmutableRun("b", newRunProperty), selection);
        }, "替换带格式文本内容");

        // 段落属性

    }
}
