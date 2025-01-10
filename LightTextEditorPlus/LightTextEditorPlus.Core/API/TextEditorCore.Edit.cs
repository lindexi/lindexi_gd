using LightTextEditorPlus.Core.Document;
using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Attributes;

namespace LightTextEditorPlus.Core;

[APIConstraint("TextEditor.Edit.Input.txt")]
public partial class TextEditorCore
{
    /// <summary>
    /// 追加一段文本，追加的文本按照段末的样式
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void AppendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        AddLayoutReason($"TextEditorCore.AppendText(string text = {text})");
        DocumentManager.AppendText(new TextRun(text));
    }

    /// <summary>
    /// 追加一段文本
    /// </summary>
    /// <param name="run"></param>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void AppendRun(IImmutableRun run)
    {
        AddLayoutReason($"TextEditorCore.AppendRun(IImmutableRun run = {run})");
        DocumentManager.AppendText(run);
    }

    /// <summary>
    /// 在当前的文本上编辑且替换。文本没有选择时，将在当前光标后面加入文本。文本有选择时，替换选择内容为输入内容
    /// </summary>
    /// <param name="text"></param>
    /// <param name="selection">传入空时，将采用 <see cref="CurrentSelection"/> 当前选择范围</param>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void EditAndReplace(string text, Selection? selection = null)
    {
        AddLayoutReason($"TextEditorCore.EditAndReplace(string text={text}, Selection? selection = {(selection?.ToString() ?? "null")}");

        TextEditorCore textEditor = this;
        DocumentManager documentManager = textEditor.DocumentManager;
        // 判断光标是否在文档末尾，且没有选择内容
        var currentSelection = selection ?? CaretManager.CurrentSelection;
        var caretOffset = currentSelection.FrontOffset;
        var isEmptyText = string.IsNullOrEmpty(text);
        if (currentSelection.IsEmpty && caretOffset.Offset == documentManager.CharCount)
        {
            if (!isEmptyText)
            {
                // 在末尾，调用追加，性能更好
                documentManager.AppendText(new TextRun(text));
            }
        }
        else
        {
            if (isEmptyText)
            {
                documentManager.EditAndReplaceRun(currentSelection, null);
            }
            else
            {
                var textRun = new TextRun(text);
                documentManager.EditAndReplaceRun(currentSelection, textRun);
            }
        }
    }

    /// <summary>
    /// 编辑和替换文本
    /// </summary>
    /// <param name="selection">为空将使用当前选择内容，当前无选择则在光标之后插入</param>
    /// <param name="run"></param>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void EditAndReplaceRun(IImmutableRun? run, Selection? selection = null)
    {
        AddLayoutReason($"TextEditorCore.EditAndReplace(IImmutableRun run = {run}, Selection selection = {selection?.ToString() ?? "null"})");
        DocumentManager.EditAndReplaceRun(selection ?? CaretManager.CurrentSelection, run);
    }

    /// <summary>
    /// 添加文本
    /// </summary>
    [Obsolete("请使用" + nameof(EditAndReplace) + "代替。此方法只是用来告诉你正确的用法是调用" + nameof(EditAndReplace) + "方法", true)]
    public void AddText()
    {
    }

    /// <summary>
    /// 在当前光标后面加入纯文本
    /// </summary>
    /// <param name="text"></param>
    [Obsolete("请使用" + nameof(EditAndReplace) + "代替。此方法只是用来告诉你正确的用法是调用" + nameof(EditAndReplace) + "方法", true)]
    public void InsertTextAfterCurrentCaretOffset(string text)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 清空文本，现在仅调试下使用
    /// </summary>
    [Obsolete("仅调试使用")]
    public void Clear()
    {
        DocumentManager.Remove(DocumentManager.GetAllDocumentSelection());
    }

    /// <summary>
    /// 退格删除，如果没有选择，则删除光标前一个字符。如果当前有选择文本，即 <see cref="CurrentSelection"/> 有范围，则删除选择内容。如需手动指定删除范围，请使用 <see cref="Remove(in Selection)"/> 方法
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void Backspace()
    {
        DocumentManager.Backspace();
    }

    /// <summary>
    /// 删除文本 Delete 删除光标后一个字符。如果当前有选择文本，即 <see cref="CurrentSelection"/> 有范围，则删除选择内容。如需手动指定删除范围，请使用 <see cref="Remove(in Selection)"/> 方法
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void Delete()
    {
        DocumentManager.Delete();
    }

    /// <summary>
    /// 删除文本，删除给定范围内的文本
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void Remove(in Selection selection)
    {
        DocumentManager.Remove(selection);
    }
}
