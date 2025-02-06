using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Utils.Patterns;

static class TextRangePatternExtension
{
    public static bool IsInRange(this TextRangePattern textRangePattern, ICharObject charObject)
    {
        return textRangePattern.IsInRange(charObject.CodePoint);
    }

    public static bool IsInRange(this TextRangePattern textRangePattern, CharData charData)
    {
        return textRangePattern.IsInRange(charData.CharObject.CodePoint);
    }
}