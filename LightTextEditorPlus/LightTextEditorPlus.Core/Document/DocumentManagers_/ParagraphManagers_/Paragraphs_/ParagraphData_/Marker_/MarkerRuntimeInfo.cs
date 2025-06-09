using System;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 项目符号运行时信息
/// </summary>
internal record MarkerRuntimeInfo
{
    public MarkerRuntimeInfo(string text, IReadOnlyRunProperty runProperty, TextMarker textMarker,
        ParagraphData paragraphData)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentNullException(nameof(text));
        }

        Text = text;
        RunProperty = runProperty;
        TextMarker = textMarker;
        ParagraphData = paragraphData;
    }

    //依靠渲染时，判断段落是否空段即可知道，不需要再加一个属性，再加一个属性还要去维护它
    ///// <summary>
    ///// 是否隐藏的标记
    ///// </summary>
    //public bool IsHidden { get; set; }

    public TextMarker TextMarker { get; }

    internal ParagraphData ParagraphData { get; }

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
            CharData charData = new CharData(textRun.GetChar(i), RunProperty);
            charData.CharLayoutData = new CharLayoutData(charData, ParagraphData);
            array[i] = charData;
        }

        return new TextReadOnlyListSpan<CharData>(array, 0, array.Length);
    }
}