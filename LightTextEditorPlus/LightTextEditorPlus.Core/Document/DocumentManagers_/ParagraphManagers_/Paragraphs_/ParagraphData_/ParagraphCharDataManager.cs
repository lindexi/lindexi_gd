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
        UpdateCharLayoutData(charData);

        CharDataList.Add(charData);

        if (CharDataList.Count == 1)
        {
            // 首个加入的字符，需要更新段落字符属性
            // 由于这个类型不提供 Insert 插入方法，只有添加和删除，所以这里判断 Count 为 1 是合理的
            _paragraph.UpdateStartRunProperty();
        }
    }

    public void AddRange(IReadOnlyList<CharData> charDataList)
    {
        if (charDataList.Count == 0)
        {
            return;
        }

        CharDataList.EnsureCapacity(CharDataList.Count + charDataList.Count);

        foreach (var charData in charDataList)
        {
            UpdateCharLayoutData(charData);
            // 分开为两个步骤，不要逐个调用 Add 方法，而是一起调用 AddRange 方法。这样的性能才足够好
            //CharDataList.Add(charData);
        }

        // List 底层没有判断 IReadOnlyCollection 接口，只判断 ICollection 接口才做数组拷贝，相对来说效率更低
        CharDataList.AddRange(charDataList);

        Debug.Assert(CharDataList.Count > 0, "由于已经判断 charDataList.Count 大于 0 因此必定可以更新首个字符属性");
        _paragraph.UpdateStartRunProperty();
    }

    private void UpdateCharLayoutData(CharData charData)
    {
        if (charData.CharLayoutData is null)
        {
            charData.CharLayoutData = new CharLayoutData(charData, _paragraph);
        }
        else
        {
            if (ReferenceEquals(charData.CharLayoutData.Paragraph, _paragraph))
            {
                // 正常情况
            }
            else
            {
                CharDataBeAddedToMultiParagraphException.Throw(charData, _paragraph);
            }
        }
    }

    public void RemoveRange(int index, int count) => CharDataList.RemoveRange(index, count);

    public IReadOnlyList<CharData> GetRange(int index, int count) => CharDataList.GetRange(index, count);

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
