using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

/// <summary>
/// 段落属性在布局过程的工具扩展
/// </summary>
internal static class ParagraphPropertyLayoutUtilsExtension
{
    /// <summary>
    /// 获取一行最大可用宽度。即 <paramref name="lineMaxWidth"/> 减去左右边距和缩进
    /// </summary>
    /// <param name="paragraphProperty"></param>
    /// <param name="lineMaxWidth">行的最大空间</param>
    /// <param name="isFirstLine">是否首行</param>
    /// <returns></returns>
    /// 没有考虑项目符号哦
    public static double GetUsableLineMaxWidth(this ParagraphProperty paragraphProperty, double lineMaxWidth, bool isFirstLine)
    {
        double indent = paragraphProperty.GetIndent(isFirstLine);

        return lineMaxWidth - paragraphProperty.LeftIndentation - paragraphProperty.RightIndentation - indent;
    }

    /// <summary>
    /// 获取缩进
    /// </summary>
    /// <param name="paragraphProperty"></param>
    /// <param name="isFirstLine">是否首行</param>
    /// <returns></returns>
    public static double GetIndent(this ParagraphProperty paragraphProperty, bool isFirstLine)
    {
        double indent = paragraphProperty.IndentType switch
        {
            // 首行缩进
            IndentType.FirstLine => isFirstLine ? paragraphProperty.Indent : 0,
            // 悬挂缩进，首行不缩进
            IndentType.Hanging => isFirstLine ? 0 : paragraphProperty.Indent,
            _ => 0
        };
        return indent;
    }

    /// <summary>
    /// 获取水平文本对齐的空白
    /// </summary>
    /// <param name="paragraphProperty"></param>
    /// <param name="usableGapWidth">可用的空白</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static TextThickness GetHorizontalTextAlignmentGapThickness(this ParagraphProperty paragraphProperty,
        double usableGapWidth)
    {
        HorizontalTextAlignment horizontalTextAlignment = paragraphProperty.HorizontalTextAlignment;
        TextThickness horizontalTextAlignmentGapThickness;
        if (horizontalTextAlignment == HorizontalTextAlignment.Left)
        {
            horizontalTextAlignmentGapThickness = new TextThickness(0, 0, usableGapWidth, 0);
        }
        else if (horizontalTextAlignment == HorizontalTextAlignment.Center)
        {
            horizontalTextAlignmentGapThickness =
                new TextThickness(usableGapWidth / 2, 0, usableGapWidth / 2, 0);
        }
        else if (horizontalTextAlignment == HorizontalTextAlignment.Right)
        {
            horizontalTextAlignmentGapThickness = new TextThickness(usableGapWidth, 0, 0, 0);
        }
        else
        {
            // 两端对齐 还不知道如何实现
            throw new NotSupportedException($"不支持 {horizontalTextAlignment} 对齐方式");
        }

        return horizontalTextAlignmentGapThickness;
    }
}