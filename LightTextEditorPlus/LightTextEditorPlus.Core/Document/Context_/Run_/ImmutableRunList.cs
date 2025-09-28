using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个 <see cref="IImmutableRun"/> 列表
/// </summary>
internal class ImmutableRunList(IEnumerable<IImmutableRun> runs)
    : ImmutableRunListBase<IImmutableRun>(runs), IImmutableRunList;