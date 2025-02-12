#if USE_WPF || USE_AVALONIA

using LightTextEditorPlus.Core;

namespace LightTextEditorPlus;

/// <summary>
/// 文本选择扩展方法
/// </summary>
public static class TextEditorSelectionExtension
{
    /// <summary>
    /// 全选文本
    /// </summary>
    public static void SelectAll(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.SelectAll();
    }

    /// <summary>
    /// 清空选择
    /// </summary>
    public static void ClearSelection(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.SelectAll();
    }
}
#endif
