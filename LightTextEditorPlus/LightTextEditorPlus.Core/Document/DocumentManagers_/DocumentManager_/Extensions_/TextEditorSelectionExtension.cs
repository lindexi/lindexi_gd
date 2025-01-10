using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;

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
}