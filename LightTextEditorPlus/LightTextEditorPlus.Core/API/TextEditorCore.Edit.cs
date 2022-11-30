using LightTextEditorPlus.Core.Document;
using System;

namespace LightTextEditorPlus.Core;

public partial class TextEditorCore
{
    /// <summary>
    /// 追加一段文本，追加的文本按照段末的样式
    /// </summary>
    public void AppendText(string text)
    {
        DocumentManager.AppendText(text);
    }

    /// <summary>
    /// 在当前的文本上编辑且替换。文本没有选择时，将在当前光标后面加入文本。文本有选择时，替换选择内容为输入内容
    /// </summary>
    /// <param name="text"></param>
    public void EditAndReplace( string text)
    {
        TextEditorCore textEditor = this;
        DocumentManager documentManager = textEditor.DocumentManager;
        // 判断光标是否在文档末尾，且没有选择内容
        var currentSelection = CaretManager.CurrentSelection;
        var caretOffset = CaretManager.CurrentCaretOffset;
        if (currentSelection.IsEmpty && caretOffset.Offset == documentManager.CharCount)
        {
            // 在末尾，调用追加，性能更好
            documentManager.AppendText(text);
        }
        else
        {
            var textRun = new TextRun(text);
            documentManager.EditAndReplaceRun(currentSelection, textRun);
        }
    }

    /// <summary>
    /// 在当前光标后面加入纯文本
    /// </summary>
    /// <param name="text"></param>
    [Obsolete("请使用" + nameof(EditAndReplace) + "代替。此方法只是用来告诉你正确的用法是调用" + nameof(EditAndReplace) + "方法", true)]
    public void InsertTextAfterCurrentCaretOffset(string text) =>
        EditAndReplace(text);
}