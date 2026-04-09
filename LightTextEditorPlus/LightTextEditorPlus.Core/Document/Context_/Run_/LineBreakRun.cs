using System;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 一个表示换行的文本段，大部分用来表示一个分段内容
/// </summary>
public sealed class LineBreakRun : IImmutableRun
{
    /// <summary>
    /// 创建一个表示换行的文本段
    /// </summary>
    /// <param name="runProperty"></param>
    public LineBreakRun(IReadOnlyRunProperty? runProperty = null)
    {
        RunProperty = runProperty;
    }

    /// <inheritdoc />
    public int Count => 0;

    /// <inheritdoc />
    public ICharObject GetChar(int index)
    {
        throw new ArgumentOutOfRangeException(nameof(index),
            ExceptionMessages.Format(nameof(LineBreakRun) + "_GetChar_NotSupported", nameof(LineBreakRun)));
    }

    /// <inheritdoc />
    public IReadOnlyRunProperty? RunProperty { get; }

    /// <inheritdoc />
    public (IImmutableRun FirstRun, IImmutableRun SecondRun) SplitAt(int index)
    {
        throw new NotSupportedException($"{nameof(LineBreakRun)} not support split.");
    }
}