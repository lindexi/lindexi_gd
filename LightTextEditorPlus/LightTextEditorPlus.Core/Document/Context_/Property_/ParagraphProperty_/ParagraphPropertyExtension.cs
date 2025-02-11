using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

internal static class ParagraphPropertyExtension
{
    /// <summary>
    /// 获取一行最大可用宽度。即 <paramref name="lineMaxWidth"/> 减去左右边距和缩进
    /// </summary>
    /// <param name="paragraphProperty"></param>
    /// <param name="lineMaxWidth">行的最大空间</param>
    /// <param name="isFirstLine">是否首行</param>
    /// <returns></returns>
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

    ///// <summary>
    ///// 获取缩进边距。包含左右边距和首行悬挂缩进
    ///// </summary>
    ///// <param name="paragraphProperty"></param>
    ///// <param name="isFirstLine">是否首行</param>
    ///// <returns></returns>
    //public static TextThickness GetIndentation(this ParagraphProperty paragraphProperty, bool isFirstLine)
    //{
    //    double leftIndentation = paragraphProperty.LeftIndentation;

    //    var indentationThickness =
    //        new TextThickness(leftIndentation, 0, paragraphProperty.RightIndentation, 0);
    //}
}