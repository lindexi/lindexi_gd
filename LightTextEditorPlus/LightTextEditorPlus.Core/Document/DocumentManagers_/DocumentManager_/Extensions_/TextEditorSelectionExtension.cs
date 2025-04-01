using System;
using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文本的选择扩展
/// </summary>
public static class TextEditorSelectionExtension
{
    /// <summary>
    /// 获取文档开始的光标
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    [TextEditorPublicAPI]
    public static CaretOffset GetDocumentStartCaretOffset(this TextEditorCore textEditor) => textEditor.DocumentManager.GetDocumentStartCaretOffset();

    /// <summary>
    /// 获取文档结尾的光标，等于 CharCount 值
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    [TextEditorPublicAPI]
    public static CaretOffset GetDocumentEndCaretOffset(this TextEditorCore textEditor) => textEditor.DocumentManager.GetDocumentEndCaretOffset();

    /// <summary>
    /// 获取选择到文档的起始，也就是 0,0 选择范围
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    [TextEditorPublicAPI]
    public static Selection GetDocumentStartSelection(this TextEditorCore textEditor) =>
        textEditor.DocumentManager.GetDocumentStartSelection();

    /// <summary>
    /// 获取选择到文档的末尾，也就是 CharCount,0 选择范围
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    [TextEditorPublicAPI]
    public static Selection GetDocumentEndSelection(this TextEditorCore textEditor) =>
        textEditor.DocumentManager.GetDocumentEndSelection();

    /// <summary>
    /// 获取对文档的全选
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    [TextEditorPublicAPI]
    public static Selection GetAllDocumentSelection(this TextEditorCore textEditor) =>
        textEditor.DocumentManager.GetAllDocumentSelection();

    /// <summary>
    /// 获取覆盖模式下的选择范围
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="inputTextLength"></param>
    /// <returns></returns>
    public static Selection GetCurrentOvertypeModeSelection(this TextEditorCore textEditor, int inputTextLength)
    {
        Selection currentSelection = textEditor.CurrentSelection;
        if (currentSelection.IsEmpty)
        {
            // 只有在无选择的情况下才能使用覆盖模式
            // 先获取所在的段落，取当前的输入文本长度和光标到段落的末尾的最小值
            var caretOffset = currentSelection.FrontOffset;
            HitParagraphDataResult hitParagraphDataResult = textEditor.DocumentManager.ParagraphManager.GetHitParagraphData(caretOffset);
            // 命中到的段落偏移量
            ParagraphCaretOffset hitOffset = hitParagraphDataResult.HitOffset;
            int charCount = hitParagraphDataResult.ParagraphData.CharCount;
            int min = Math.Min(inputTextLength, charCount - hitOffset.Offset);
            return new Selection(caretOffset, min);
        }
        else
        {
            // 有选择的情况下，如果选择范围和输入文本长度不一致，那就不是覆盖模式
            return currentSelection;
        }
    }
}