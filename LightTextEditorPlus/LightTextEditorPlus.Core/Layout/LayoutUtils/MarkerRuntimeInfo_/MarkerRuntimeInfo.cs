using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

// todo 考虑直接就是 TextRun 类型好了，或类似的类型
internal readonly record struct MarkerRuntimeInfo(string Text, IReadOnlyRunProperty RunProperty)
{
    public TextReadOnlyListSpan<CharData> ToCharDataList()
    {
        TextRun textRun = new TextRun(Text,RunProperty);
        var array = new CharData[textRun.Count];
        for (int i = 0; i < textRun.Count; i++)
        {
            array[i] = new CharData(textRun.GetChar(i), RunProperty);
        }

        return new TextReadOnlyListSpan<CharData>(array, 0, array.Length);
    }
}