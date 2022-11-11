using LightTextEditorPlus.Core.Carets;
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