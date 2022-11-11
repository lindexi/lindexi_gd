using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.DocumentManagers;

namespace LightTextEditorPlus.Core;

public static class TextEditorCoreSelectionExtension
{
    /// <summary>
    /// 全选文本
    /// </summary>
    public static void SelectAll(this TextEditorCore textEditor)
    {
        DocumentManager documentManager = textEditor.DocumentManager;
        var allDocumentSelection = documentManager.GetAllDocumentSelection();
        documentManager.SetSelection(allDocumentSelection);
    }

    /// <summary>
    /// 清空选择
    /// </summary>
    public static void ClearSelection(this TextEditorCore textEditor)
    {
        // todo 确认清空选择的时候，光标应该在哪
        DocumentManager documentManager = textEditor.DocumentManager;

        var selection = new Selection(documentManager.CurrentCaretOffset, 0);
        documentManager.SetSelection(selection);
    }
}

/// <summary>
/// 文档的编辑扩展
/// </summary>
public static class TextEditorCoreEditExtension
{
    /// <summary>
    /// 在当前光标后面加入纯文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="text"></param>
    public static void InsertTextAfterCurrentCaretOffset(this TextEditorCore textEditor, string text)
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
}