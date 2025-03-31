using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文档的选择扩展
/// </summary>
/// 这是给框架内使用的
internal static class DocumentManagerSelectionExtension
{
    /// <summary>
    /// 获取文档开始的光标
    /// </summary>
    /// <param name="documentManager"></param>
    /// <returns></returns>
    public static CaretOffset GetDocumentStartCaretOffset(this DocumentManager documentManager) => new CaretOffset(0);

    /// <summary>
    /// 获取文档结尾的光标，等于 CharCount 值
    /// </summary>
    /// <param name="documentManager"></param>
    /// <returns></returns>
    public static CaretOffset GetDocumentEndCaretOffset(this DocumentManager documentManager) => new CaretOffset(documentManager.CharCount);

    /// <summary>
    /// 获取选择到文档的起始，也就是 0,0 选择范围
    /// </summary>
    /// <param name="documentManager"></param>
    /// <returns></returns>
    public static Selection GetDocumentStartSelection(this DocumentManager documentManager) =>
        new Selection(documentManager.GetDocumentStartCaretOffset(), 0);

    /// <summary>
    /// 获取选择到文档的末尾，也就是 CharCount,0 选择范围
    /// </summary>
    /// <param name="documentManager"></param>
    /// <returns></returns>
    public static Selection GetDocumentEndSelection(this DocumentManager documentManager) =>
        new Selection(documentManager.GetDocumentEndCaretOffset(), 0);

    /// <summary>
    /// 获取对文档的全选
    /// </summary>
    /// <param name="documentManager"></param>
    /// <returns></returns>
    public static Selection GetAllDocumentSelection(this DocumentManager documentManager) =>
        new Selection(documentManager.GetDocumentStartCaretOffset(), documentManager.GetDocumentEndCaretOffset());

    /// <summary>
    /// 传入的 <paramref name="selection"/> 是否是全文选择
    /// </summary>
    /// <param name="documentManager"></param>
    /// <param name="selection"></param>
    /// <returns></returns>
    public static bool IsAllDocumentSelection(this DocumentManager documentManager, in Selection selection)
    {
        if (selection.FrontOffset.Offset != 0)
        {
            // 短路代码，如果不是从 0 开始的选择，那么肯定不是全文选择
            return false;
        }

        return documentManager.GetAllDocumentSelection().Equals(selection);
    }
}