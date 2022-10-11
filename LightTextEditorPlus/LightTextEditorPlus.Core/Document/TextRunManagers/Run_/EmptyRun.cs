using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 一个空白的文本段，大部分用来表示一个分段内容
/// </summary>
public class EmptyRun : IRun
{
    public EmptyRun(IReadOnlyRunProperty? runProperty = null)
    {
        RunProperty = runProperty;
    }

    public int Count => 0;
    public ICharObject GetChar(int index)
    {
        throw new ArgumentOutOfRangeException(nameof(index), $"禁止对{nameof(EmptyRun)}获取字符对象");
    }

    public IReadOnlyRunProperty? RunProperty { get; }
}