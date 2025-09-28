using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus.Demo.Business.RichTextCases;

partial class RichTextCaseProvider
{
    private partial void OnInit()
    {
        AddCommonCase();

        // 示例
        Add(editor =>
        {
            TextEditor textEditor = editor;
            textEditor.AppendRun(new ImmutableTextRun("abc", textEditor.CreateRunProperty(property => property with
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
            textEditor.EditAndReplaceRun(new ImmutableTextRun("b", newRunProperty), selection);
        }, "替换带格式文本内容");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            textEditor.Text = "abc";
            // 选中 'b' 这个字符
            Selection selection = new Selection(new CaretOffset(1), 1);
            textEditor.SetForeground(new ImmutableBrush(Brushes.Red), selection);
        }, "设置文本字符前景色");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            RunProperty styleRunProperty = textEditor.StyleRunProperty;
            textEditor.AppendRun(new ImmutableTextRun("a", styleRunProperty with
            {
                Foreground = new ImmutableBrush(Brushes.Red)
            }));
            textEditor.AppendRun(new ImmutableTextRun("b", styleRunProperty with
            {
                Foreground = new ImmutableBrush(Brushes.Green)
            }));
            textEditor.AppendRun(new ImmutableTextRun("c", styleRunProperty with
            {
                Foreground = new ImmutableBrush(Brushes.Blue)
            }));

            // 这是最全的设置文本字符属性的方式
            textEditor.ConfigRunProperty(runProperty => runProperty with
            {
                // 此方式是传入委托，将会进入多次，允许只修改某几个属性，而保留其他原本的字符属性
                // 如这里没有碰颜色属性，则依然能够保留原本字符的颜色
                FontSize = 30,
                FontName = new FontName("Times New Roman"),
            }, textEditor.GetAllDocumentSelection());
        }, "配置文本字符属性");

        Add(editor =>
        {
            TextEditor textEditor = editor;

            RunProperty styleRunProperty = textEditor.StyleRunProperty;
            textEditor.AppendRun(new ImmutableTextRun("a", styleRunProperty with
            {
                Foreground = new ImmutableBrush(Brushes.Red)
            }));
            textEditor.AppendRun(new ImmutableTextRun("b", styleRunProperty with
            {
                Foreground = new ImmutableBrush(Brushes.Green)
            }));
            textEditor.AppendRun(new ImmutableTextRun("c", styleRunProperty with
            {
                Foreground = new ImmutableBrush(Brushes.Blue)
            }));

            // 这是最全的设置文本字符属性的方式
            RunProperty runProperty = textEditor.CreateRunProperty(runProperty => runProperty with
            {
                // 此方式是传入委托，将会进入多次，允许只修改某几个属性，而保留其他原本的字符属性
                FontSize = 30,
                FontName = new FontName("Times New Roman"),
            });

            // 此时会使用 runProperty 覆盖全部的文本字符属性
            textEditor.SetRunProperty(runProperty, textEditor.GetAllDocumentSelection());
        }, "设置文本字符属性");
    }
}
