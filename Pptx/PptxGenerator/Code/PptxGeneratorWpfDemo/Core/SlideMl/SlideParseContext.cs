using System.Collections.Generic;

namespace PptxGenerator;

/// <summary>
/// SlideML 解析上下文，承载解析过程中的诊断信息收集器。
/// </summary>
internal sealed class SlideParseContext
{
    /// <summary>
    /// 解析过程中产生的警告信息列表。
    /// </summary>
    public List<string> Warnings { get; } = [];

    /// <summary>
    /// 解析过程中产生的错误信息列表（预留）。
    /// </summary>
    public List<string> Errors { get; } = [];
}
