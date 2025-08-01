#if TopApiTextEditorDefinition
using System.Collections;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

#if USE_WPF
#else
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
#endif

namespace LightTextEditorPlus.Document;

/// <summary>
/// 对 <see cref="CharData"/> 的扩展
/// </summary>
internal static class CharInfoConverter
{
    /// <summary>
    /// 转换为 <see cref="CharInfo"/> 结构体
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    public static CharInfo ToCharInfo(this CharData charData) => new CharInfo(charData.CharObject, charData.RunProperty.AsRunProperty());

    internal static IReadOnlyList<CharInfo> ToCharInfoList(in TextReadOnlyListSpan<CharData> list)
    {
        return new CharInfoList(list);
    }
}

file class CharInfoList : IReadOnlyList<CharInfo>
{
    public CharInfoList(in TextReadOnlyListSpan<CharData> list)
    {
        _list = list;
    }

    private readonly TextReadOnlyListSpan<CharData> _list;

    public IEnumerator<CharInfo> GetEnumerator()
    {
        return new CharInfoListEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _list.Count;

    public CharInfo this[int index] => _list[index].ToCharInfo();
}

file class CharInfoListEnumerator : IEnumerator<CharInfo>
{
    public CharInfoListEnumerator(CharInfoList list)
    {
        _list = list;
    }

    private readonly CharInfoList _list;
    private int _index = -1;

    public bool MoveNext()
    {
        _index++;
        if (_index >= _list.Count)
        {
            return false;
        }

        return true;
    }

    public void Reset()
    {
        _index = -1;
    }

    public CharInfo Current => _list[_index];

    public void Dispose()
    {
    }

    object IEnumerator.Current => Current;
}

#if USE_WPF
#else
file static class RunPropertyExtension
{
    public static RunProperty AsRunProperty(this IReadOnlyRunProperty runProperty) => (RunProperty) runProperty;
}
#endif

#endif
