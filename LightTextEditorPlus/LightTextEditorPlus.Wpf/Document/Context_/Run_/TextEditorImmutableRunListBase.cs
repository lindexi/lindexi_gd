using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本列表
/// </summary>
public abstract class TextEditorImmutableRunListBase<T> : IImmutableRunList
    where T: IImmutableRun
{
    /// <summary>
    /// 创建不可变的文本列表
    /// </summary>
    protected TextEditorImmutableRunListBase(IEnumerable<T> runs)
    {
        _runs = runs.ToImmutableArray();
    }

    private readonly ImmutableArray<T> _runs;

    /// <inheritdoc />
    public int CharCount => _runs.Sum(t => t.Count);

    /// <inheritdoc />
    public int RunCount => _runs.Length;

    /// <summary>
    /// 获取文本段
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T GetRun(int index)
    {
        return _runs[index];
    }

    IImmutableRun IImmutableRunList.GetRun(int index)
    {
        return _runs[index];
    }
}