using System.Collections.Generic;

namespace AgentLib.Tools;

/// <summary>
/// 表示对单个文件进行模式匹配的汇总结果，包含所有命中行信息以及是否因命中行数过多而被截断。
/// </summary>
public readonly struct WorkspaceFileMatchResults
{
    /// <summary>
    /// 初始化 <see cref="WorkspaceFileMatchResults"/> 的新实例。
    /// </summary>
    /// <param name="matches">命中行匹配结果列表。</param>
    /// <param name="isTruncated">命中行数是否被截断。</param>
    public WorkspaceFileMatchResults(IReadOnlyList<WorkspaceFileMatchLineResult> matches, bool isTruncated)
    {
        Matches = matches;
        IsTruncated = isTruncated;
    }

    /// <summary>
    /// 命中行匹配结果列表。
    /// </summary>
    public IReadOnlyList<WorkspaceFileMatchLineResult> Matches { get; }

    /// <summary>
    /// 指示命中行数是否因超过限制而被截断。
    /// </summary>
    public bool IsTruncated { get; }
}