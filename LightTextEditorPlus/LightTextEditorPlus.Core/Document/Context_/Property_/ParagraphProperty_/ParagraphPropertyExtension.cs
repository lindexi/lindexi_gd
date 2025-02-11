using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

internal static class ParagraphPropertyExtension
{
    /// <summary>
    /// 获取段落的左缩进值。因为段落的缩进值是存在首行缩进和悬挂缩进两种情况的，所以需要根据是否是首行来判断
    /// </summary>
    /// <param name="property"></param>
    /// <param name="isFirstLine">是否首行</param>
    /// <returns></returns>
    public static double GetLeftIndentationValue(this ParagraphProperty property, bool isFirstLine)
    {
        return property.IndentType switch
        {
            IndentType.Hanging => property.LeftIndentation,
            IndentType.FirstLine => isFirstLine ? property.LeftIndentation : 0,
            _ => 0,
        };
    }
}