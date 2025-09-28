using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本列表
/// </summary>
public class SkiaImmutableRunList(IEnumerable<SkiaImmutableRun> runs)
    : ImmutableRunListBase<SkiaImmutableRun>(runs);