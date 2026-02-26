#if USE_WPF || USE_AVALONIA

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

namespace LightTextEditorPlus;

/// <inheritdoc cref="Core.TextEditorSelectionExtension"/>
public static class TextEditorSelectionExtension
{
    /// <inheritdoc cref="Core.TextEditorSelectionExtension.SelectAll"/>
    public static void SelectAll(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.SelectAll();
    }

    /// <inheritdoc cref="Core.TextEditorSelectionExtension.ClearSelection"/>
    public static void ClearSelection(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.SelectAll();
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorSelectionExtension.GetDocumentStartCaretOffset"/>
    public static CaretOffset GetDocumentStartCaretOffset
        (this TextEditor textEditor) => textEditor.TextEditorCore.GetDocumentStartCaretOffset();

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorSelectionExtension.GetDocumentEndCaretOffset"/>
    public static CaretOffset GetDocumentEndCaretOffset
        (this TextEditor textEditor) => textEditor.TextEditorCore.GetDocumentEndCaretOffset();

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorSelectionExtension.GetDocumentStartSelection"/>
    public static Selection GetDocumentStartSelection
        (this TextEditor textEditor) => textEditor.TextEditorCore.GetDocumentStartSelection();

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorSelectionExtension.GetDocumentEndSelection"/>
    public static Selection GetDocumentEndSelection
        (this TextEditor textEditor) => textEditor.TextEditorCore.GetDocumentEndSelection();

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorSelectionExtension.GetAllDocumentSelection"/>
    public static Selection GetAllDocumentSelection
        (this TextEditor textEditor) => textEditor.TextEditorCore.GetAllDocumentSelection();

    /// <summary>
    /// 获取传入光标所在的单词选择范围
    /// </summary>
    /// <returns></returns>
    public static Selection GetCaretWord(this TextEditor textEditor, in CaretOffset caretOffset)
    {
        IWordDivider wordDivider = textEditor.TextEditorPlatformProvider.GetWordDivider();
        var result = wordDivider.GetCaretWord(new GetCaretWordArgument(caretOffset, textEditor.TextEditorCore));
        return result.WordSelection;
    }
}
#endif