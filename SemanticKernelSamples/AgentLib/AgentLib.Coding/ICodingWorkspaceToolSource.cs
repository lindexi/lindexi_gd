using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 按代码工作区创建一组附加编程工具。
/// </summary>
public interface ICodingWorkspaceToolSource
{
    /// <summary>
    /// 为指定代码工作区创建工具。
    /// </summary>
    /// <param name="workspacePath">代码工作区的完整路径。</param>
    /// <returns>绑定该工作区的工具集合。</returns>
    IReadOnlyList<AITool> CreateTools(string workspacePath);
}
