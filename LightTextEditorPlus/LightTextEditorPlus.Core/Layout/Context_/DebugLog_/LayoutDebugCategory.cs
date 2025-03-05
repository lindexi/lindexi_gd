namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 布局过程中的调试分类信息
/// </summary>
public enum LayoutDebugCategory
{
    /// <summary>
    /// 整个文档
    /// </summary>
    Document,

    /// <summary>
    /// 寻找脏数据开始，准备过程
    /// </summary>
    FindDirty,

    /// <summary>
    /// 预布局的文档部分
    /// </summary>
    PreDocument,

    /// <summary>
    /// 预布局的段落部分
    /// </summary>
    PreParagraph,

    /// <summary>
    /// 预布局的整行部分
    /// </summary>
    PreWholeLine,

    /// <summary>
    /// 预布局的行内单字词部分
    /// </summary>
    PreSingleCharLine,

    /// <summary>
    /// 分词换行部分
    /// </summary>
    PreDivideWord,

    /// <summary>
    /// 回溯的文档部分
    /// </summary>
    FinalDocument,

    /// <summary>
    /// 回溯的段落部分
    /// </summary>
    FinalParagraph,

    /// <summary>
    /// 回溯的行部分
    /// </summary>
    FinalLine,
}