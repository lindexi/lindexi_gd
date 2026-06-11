using System.Collections.Generic;

namespace PptxGenerator;

/// <summary>
/// SlideML 解析上下文，承载解析过程中的诊断信息收集器。
/// </summary>
internal sealed class SlideParseContext
{
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    /// <summary>
    /// 添加一条警告信息。
    /// </summary>
    /// <param name="message">警告信息。</param>
    public void AddWarning(string message) => _warnings.Add(message);

    /// <summary>
    /// 添加多条警告信息。
    /// </summary>
    /// <param name="messages">警告信息集合。</param>
    public void AddWarnings(IEnumerable<string> messages) => _warnings.AddRange(messages);

    /// <summary>
    /// 添加一条错误信息。
    /// </summary>
    /// <param name="message">错误信息。</param>
    public void AddError(string message) => _errors.Add(message);

    /// <summary>
    /// 添加多条错误信息。
    /// </summary>
    /// <param name="messages">错误信息集合。</param>
    public void AddErrors(IEnumerable<string> messages) => _errors.AddRange(messages);

    /// <summary>
    /// 获取当前收集的所有警告信息。
    /// </summary>
    /// <returns>警告信息列表。</returns>
    public IReadOnlyList<string> GetWarnings() => _warnings;

    /// <summary>
    /// 获取当前收集的所有错误信息。
    /// </summary>
    /// <returns>错误信息列表。</returns>
    public IReadOnlyList<string> GetErrors() => _errors;
}