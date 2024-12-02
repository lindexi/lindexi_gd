using System;
using System.Collections.Generic;
using System.Diagnostics;

using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落里的字符管理
/// </summary>
class ParagraphCharDataManager
{
    public ParagraphCharDataManager(ParagraphData paragraph)
    {
        _paragraph = paragraph;
    }

    // 这个字段没有什么用，更多是给调试使用，防止一个 ParagraphCharDataManager 被在多个段落使用
    private readonly ParagraphData _paragraph;

    // 不公开，后续也许会更换数据类型
    private List<CharData> CharDataList { get; } = new List<CharData>();

    public int CharCount => CharDataList.Count;

    public void Add(CharData charData)
    {
        Debug.Assert(charData.CharLayoutData is null, "一个 CharData 不会被加入两次");
        charData.CharLayoutData = new CharLayoutData(charData, _paragraph);
        CharDataList.Add(charData);
    }

    public void AddRange(IEnumerable<CharData> charDataList)
    {
        foreach (var charData in charDataList)
        {
            Add(charData);
        }

        //CharDataList.AddRange(charDataList);
    }

    public void RemoveRange(int index, int count) => CharDataList.RemoveRange(index, count);

    public IList<CharData> GetRange(int index, int count) => CharDataList.GetRange(index, count);

    public TextReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start) =>
        ToReadOnlyListSpan(start, CharDataList.Count - start);

    public TextReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start, int length) =>
        new TextReadOnlyListSpan<CharData>(CharDataList, start, length);

    public CharData GetCharData(int offset)
    {
        try
        {
            return CharDataList[offset];
        }
        catch (ArgumentOutOfRangeException e)
        {
            throw new GetCharDataOutOfRangeException(_paragraph, CharDataList, offset, e);
        }
    }
}