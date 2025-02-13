#if USE_WPF || USE_AVALONIA

using LightTextEditorPlus.Core;

namespace LightTextEditorPlus;

/// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCoreSelectionExtension"/>
public static class TextEditorSelectionExtension
{
    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCoreSelectionExtension.SelectAll"/>
    public static void SelectAll(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.SelectAll();
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCoreSelectionExtension.ClearSelection"/>
    public static void ClearSelection(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.SelectAll();
    }
}
#endif
