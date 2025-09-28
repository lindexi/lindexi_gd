using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本列表
/// </summary>
public class TextEditorImmutableTextRunList : ImmutableRunListBase<ImmutableTextRun>
{
    /// <summary>
    /// 创建不可变的文本列表
    /// </summary>
    public TextEditorImmutableTextRunList(IEnumerable<ImmutableTextRun> runs) : base(runs)
    {
    }
}