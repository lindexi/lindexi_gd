using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Tests;

static class ReadOnlyListSpanExtension
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