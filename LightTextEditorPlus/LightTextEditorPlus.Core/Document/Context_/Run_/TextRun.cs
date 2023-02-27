using System;
using LightTextEditorPlus.Core.Exceptions;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符串文本片段
/// </summary>
public class TextRun : IImmutableTextRun, IImmutableRunList
{
    /// <summary>
    /// 创建字符串文本片段
    /// </summary>
    /// <param name="text"></param>
    /// <param name="runProperty"></param>
    public TextRun(string text, IReadOnlyRunProperty? runProperty = null)
    {
        Text = text;
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