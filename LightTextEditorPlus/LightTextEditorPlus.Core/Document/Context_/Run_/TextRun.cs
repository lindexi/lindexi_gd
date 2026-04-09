using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符串文本片段
/// </summary>
public class TextRun : IImmutableTextRun
    // 同时让 TextRun 继承 IImmutableRunList 的原因是 TextRun 使用次数特别多。而且每次都是单个 TextRun 在使用。如果每次都需要为 TextRun 分配一个新的对象，那自然是比较亏的
    // 让 TextRun 继承 IImmutableRunList 可以共用一个对象，减少对象分配
    , IImmutableRunList
{
    /// <summary>
    /// 创建字符串文本片段
    /// </summary>
    /// <param name="text"></param>
    /// <param name="runProperty"></param>
    public TextRun(string text, IReadOnlyRunProperty? runProperty = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        //Text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        Text = text;
        RunProperty = runProperty;

        CharObjectList = TextCharObjectCreator.TextToCharObjectList(text);
    }

    private IReadOnlyList<ICharObject> CharObjectList { get; }

    /// <summary>
    /// 文本字符串
    /// </summary>
    public string Text { get; }

    /// <inheritdoc />
    public int Count => CharObjectList.Count;

    /// <inheritdoc />
    public ICharObject GetChar(int index)
    {
        return CharObjectList[index];
    }

    /// <inheritdoc />
    public IReadOnlyRunProperty? RunProperty { get; }

    /// <inheritdoc />
    public (IImmutableRun FirstRun, IImmutableRun SecondRun) SplitAt(int index)
    {
        if (index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index == 0)
        {
            throw new ArgumentException($"{nameof(index)} must more than 0");
        }

        var listSpan = new TextReadOnlyListSpan<ICharObject>(CharObjectList, 0, CharObjectList.Count);
        var firstSpan = listSpan.Slice(0, index);
        var secondSpan = listSpan.Slice(index);
        return (new CharObjectSpanTextRun(firstSpan, RunProperty), new CharObjectSpanTextRun(secondSpan, RunProperty));
    }

    /// <summary>
    /// 拆分文本片段
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public IImmutableRun Slice(int start, int length)
    {
        if (start < 0 || start >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }
        if (length < 0 || start + length > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var listSpan = new TextReadOnlyListSpan<ICharObject>(CharObjectList, start, length);
        return new CharObjectSpanTextRun(listSpan, RunProperty);
    }

    /// <inheritdoc />
    public override string ToString() => Text;

    int IImmutableRunList.CharCount => Count;
    int IImmutableRunList.RunCount => 1;

    IImmutableRun IImmutableRunList.GetRun(int index)
    {
        if (index != 0)
        {
            throw new TextEditorInnerException($"获取只有单个 Run 的 {nameof(TextRun)} 时，传入的 {nameof(index)} 参数是 {index} 而不是 0 的值");
        }

        return this;
    }
}
