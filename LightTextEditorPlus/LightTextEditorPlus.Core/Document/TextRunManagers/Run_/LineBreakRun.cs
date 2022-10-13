using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 一个表示换行的文本段，大部分用来表示一个分段内容
/// </summary>
public class LineBreakRun : IRun
{
    public LineBreakRun(IReadOnlyRunProperty? runProperty = null)
    {
        RunProperty = runProperty;
    }

    public int Count => 0;
    public ICharObject GetChar(int index)
    {
        throw new ArgumentOutOfRangeException(nameof(index), $"禁止对{nameof(LineBreakRun)}获取字符对象");
    }

    public IReadOnlyRunProperty? RunProperty { get; }
    public (IRun FirstRun, IRun SecondRun) SplitAt(int index)
    {
        throw new NotSupportedException($"{nameof(LineBreakRun)} not support split.");
    }
}