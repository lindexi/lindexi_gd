using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.DocumentManagers;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文档的编辑扩展
/// </summary>
public static class DocumentManagerEditExtension
{
    /// <summary>
    /// 在当前光标后面加入纯文本
    /// </summary>
    /// <param name="documentManager"></param>
    /// <param name="text"></param>
    public static void InsertTextAfterCurrentCaretOffset(this DocumentManager documentManager, string text)
    {
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