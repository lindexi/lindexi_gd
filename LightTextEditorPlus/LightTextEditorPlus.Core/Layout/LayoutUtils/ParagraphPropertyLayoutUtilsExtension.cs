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
    /// 获取水平文本对齐的空白
    /// </summary>
    /// <param name="paragraphProperty"></param>
    /// <param name="remainingGapWidth">剩余的空白</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static TextThickness GetHorizontalTextAlignmentGapThickness(this ParagraphProperty paragraphProperty,
        double remainingGapWidth)
    {
        HorizontalTextAlignment horizontalTextAlignment = paragraphProperty.HorizontalTextAlignment;
        TextThickness horizontalTextAlignmentGapThickness;
        if (horizontalTextAlignment == HorizontalTextAlignment.Left)
        {
            horizontalTextAlignmentGapThickness = new TextThickness(0, 0, remainingGapWidth, 0);
        }
        else if (horizontalTextAlignment == HorizontalTextAlignment.Center)
        {
            horizontalTextAlignmentGapThickness =
                new TextThickness(remainingGapWidth / 2, 0, remainingGapWidth / 2, 0);
        }
        else if (horizontalTextAlignment == HorizontalTextAlignment.Right)
        {
            horizontalTextAlignmentGapThickness = new TextThickness(remainingGapWidth, 0, 0, 0);
        }
        else
        {
            // 两端对齐 还不知道如何实现
            throw new NotSupportedException($"不支持 {horizontalTextAlignment} 对齐方式");
        }

        return horizontalTextAlignmentGapThickness;
    }
}