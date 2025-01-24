using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文本选择扩展方法
/// </summary>
public static class TextEditorCoreSelectionExtension
{
    /// <summary>
    /// 全选文本
    /// </summary>
    public static void SelectAll(this TextEditorCore textEditor)
    {
        DocumentManager documentManager = textEditor.DocumentManager;
        var allDocumentSelection = documentManager.GetAllDocumentSelection();
        textEditor.CaretManager.SetSelection(allDocumentSelection);
    }

    /// <summary>
    /// 清空选择
    /// </summary>
    public static void ClearSelection(this TextEditorCore textEditor)
    {
        // todo 确认清空选择的时候，光标应该在哪

        var selection = new Selection(textEditor.CaretManager.CurrentCaretOffset, 0);
        textEditor.CaretManager.SetSelection(selection);
    }
}
