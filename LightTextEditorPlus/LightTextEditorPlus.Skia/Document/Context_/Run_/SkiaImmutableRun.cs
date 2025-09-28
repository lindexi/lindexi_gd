using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本
/// </summary>
public class SkiaImmutableRun : IImmutableRun
{
    /// <summary>
    /// 创建不可变的文本
    /// </summary>
    /// 如果想创建字符串的 Run 推荐使用 <see cref="SkiaTextRun"/> 类型
    public SkiaImmutableRun(SkiaTextRunProperty runProperty, IEnumerable<ICharObject> charObjects)
        : this(runProperty, charObjects.ToImmutableArray())
    {
    }

    /// <summary>
    /// 创建不可变的文本
    /// </summary>
    public SkiaImmutableRun(SkiaTextRunProperty runProperty, ImmutableArray<ICharObject> charObjectArray)
    {
        _charObjectArray = charObjectArray;
        RunProperty = runProperty;
    }

    private readonly ImmutableArray<ICharObject> _charObjectArray;

    /// <inheritdoc />
    public int Count => _charObjectArray.Length;

    /// <inheritdoc />
    public ICharObject GetChar(int index)
    {
        return _charObjectArray[index];
    }

    /// <summary>
    /// 定义的样式
    /// </summary>
    public SkiaTextRunProperty RunProperty { get; }

    /// <inheritdoc />
    IReadOnlyRunProperty IImmutableRun.RunProperty => RunProperty;

    /// <inheritdoc />
    public (IImmutableRun FirstRun, IImmutableRun SecondRun) SplitAt(int index)
    {
        var first = new SkiaImmutableRun(RunProperty, _charObjectArray.Take(index));
        var second = new SkiaImmutableRun(RunProperty, _charObjectArray.Skip(index));
        return (first, second);
    }
}