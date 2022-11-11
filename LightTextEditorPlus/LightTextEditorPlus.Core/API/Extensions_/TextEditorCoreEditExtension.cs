using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.DocumentManagers;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文档的编辑扩展
/// </summary>
public static class TextEditorCoreEditExtension
{
    /// <summary>
    /// 在当前的文本上编辑且替换。文本没有选择时，将在当前光标后面加入文本。文本有选择时，替换选择内容为输入内容
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="text"></param>
    public static void EditAndReplace(this TextEditorCore textEditor, string text)
    {
        DocumentManager documentManager = textEditor.DocumentManager;
        // 判断光标是否在文档末尾，且没有选择内容
        var currentSelection = documentManager.CurrentSelection;
        var caretOffset = documentManager.CurrentCaretOffset;
        if (currentSelection.IsEmpty && caretOffset.Offset == documentManager.CharCount)
        {
            // 在末尾，调用追加，性能更好
            documentManager.AppendText(text);
        }
        else
        {
            documentManager.EditAndReplaceRun(currentSelection, new TextRun(text));
        }
    }

    /// <summary>
    /// 在当前光标后面加入纯文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="text"></param>
    [Obsolete("请使用" + nameof(EditAndReplace) + "代替。此方法只是用来告诉你正确的用法是调用" + nameof(EditAndReplace) + "方法")]
    public static void InsertTextAfterCurrentCaretOffset(this TextEditorCore textEditor, string text) =>
        textEditor.EditAndReplace(text);
}