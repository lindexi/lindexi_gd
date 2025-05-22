using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

internal record MarkerRuntimeInfo
{
    public MarkerRuntimeInfo(string text, IReadOnlyRunProperty runProperty)
    {
        Text = text;
        RunProperty = runProperty;
    }

    public double MarkerIndentation { get; set; } = 0;

    public TextReadOnlyListSpan<CharData> CharDataList => _charDataList ??= ToCharDataList();
    private TextReadOnlyListSpan<CharData>? _charDataList;

    public string Text { get; init; }
    public IReadOnlyRunProperty RunProperty { get; init; }

    private TextReadOnlyListSpan<CharData> ToCharDataList()
    {
        if (string.IsNullOrEmpty(Text))
        {
            return new TextReadOnlyListSpan<CharData>([], 0, 0);
        }

        TextRun textRun = new TextRun(Text, RunProperty);
        var array = new CharData[textRun.Count];
        for (int i = 0; i < textRun.Count; i++)
        {
            array[i] = new CharData(textRun.GetChar(i), RunProperty);
        }

        return new TextReadOnlyListSpan<CharData>(array, 0, array.Length);
    }
}