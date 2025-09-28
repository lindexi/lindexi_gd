using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本列表
/// </summary>
public class ImmutableRunList : ImmutableRunListBase<ImmutableRun>
{
    /// <summary>
    /// 创建不可变的文本列表
    /// </summary>
    public ImmutableRunList(IEnumerable<ImmutableRun> runs) : base(runs)
    {
    }
}