using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

internal static class ParagraphPropertyExtension
{
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