using System;

using static LightTextEditorPlus.Core.Layout.LayoutDebugCategory;

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
    /// 预布局的段落部分，开始
    /// </summary>
    PreParagraphStart,

    /// <summary>
    /// 预布局的段落部分，过程
    /// </summary>
    PreParagraph,

    /// <summary>
    /// 段落缩进、项目符号缩进
    /// </summary>
    PreIndent,

    /// <summary>
    /// 项目符号缩进
    /// </summary>
    PreMarkerIndent,

    /// <summary>
    /// 预布局的整行部分，开始
    /// </summary>
    PreWholeLineStart,

    /// <summary>
    /// 预布局的整行部分，过程
    /// </summary>
    PreWholeLine,

    /// <summary>
    /// 预布局的在整行部分的行距计算
    /// </summary>
    PreLineSpacingInWholeLine,

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

/// <summary>
/// 布局过程中的调试分类信息扩展
/// </summary>
public static class LayoutDebugCategoryExtension
{
    /// <summary>
    /// 将布局调试分类转换为日志缩进计数
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public static int ToLogPadCount(this LayoutDebugCategory category)
    {
        return ToPad(category);
    }

    private static int ToPad(LayoutDebugCategory category)
    {
        return category switch
        {
            LayoutDebugCategory.Document => 0,
            FindDirty => 1,

            PreDocument => 1,
            PreParagraphStart => After(PreDocument),
            PreParagraph => After(PreParagraphStart),
            PreIndent => After(PreParagraph),
            PreMarkerIndent => After(PreIndent),
            PreWholeLineStart => After(PreParagraph),
            PreWholeLine => After(PreWholeLineStart),
            PreLineSpacingInWholeLine => After(PreWholeLine),
            PreSingleCharLine => After(PreWholeLine),
            PreDivideWord => After(PreDivideWord),
            FinalDocument => 1,
            FinalParagraph => After(FinalDocument),
            FinalLine => After(FinalParagraph),

            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };

        static int After(LayoutDebugCategory category)
        {
            return ToPad(category) + 1;
        }
    }
}