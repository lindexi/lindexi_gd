using LightTextEditorPlus.Core.Primitive.Collections;
using System.Text;

namespace LightTextEditorPlus.Core.Document;

static class CharDataReadOnlyListSpanExtension
{
    public static string ToText(this ReadOnlyListSpan<CharData> list)
    {
        var stringBuilder = new StringBuilder();
        foreach (CharData charData in list)
        {
            stringBuilder.Append(charData.CharObject.ToText());
        }
        return stringBuilder.ToString();
    }
}