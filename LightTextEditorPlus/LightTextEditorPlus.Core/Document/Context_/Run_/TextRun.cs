using System;

using LightTextEditorPlus.Core.Exceptions;

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
        Text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        RunProperty = runProperty;
    }

    /// <summary>
    /// 文本字符串
    /// </summary>
    public string Text { get; }

    /// <inheritdoc />
    public int Count => Text.Length;

    /// <inheritdoc />
    public ICharObject GetChar(int index)
    {
        // todo 考虑 emoij 的情况，多个 char 组成一个字符
        return TextCharObjectCreator.CreateCharObject(Text, index);
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

        var firstStart = 0;
        var firstLength = index;

        var secondStart = 0 + index;
        var secondLength = Count - index;

        return (new SpanTextRun(Text, firstStart, firstLength, RunProperty),
            new SpanTextRun(Text, secondStart, secondLength));
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