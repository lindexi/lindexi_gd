using System.Collections.Generic;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本列表
/// </summary>
public class ImmutableTextRunList : ImmutableRunListBase<ImmutableTextRun>
{
    /// <summary>
    /// 创建不可变的文本列表
    /// </summary>
    public ImmutableTextRunList(IEnumerable<ImmutableTextRun> runs) : base(runs)
    {
    }
}