using AgentLib.Model;
using AgentLib.Tools;

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

    /// <summary>
    /// 为指定代码工作区创建工具及其展示摘要规则。
    /// </summary>
    /// <param name="workspacePath">代码工作区的完整路径。</param>
    /// <returns>绑定该工作区的工具注册集合。</returns>
    IReadOnlyList<ToolRegistration> CreateToolRegistrations(string workspacePath) =>
        CreateTools(workspacePath)
            .Select(tool => new ToolRegistration(tool))
            .ToArray();

    /// <summary>
    /// 为单次编程运行创建需要当前消息上下文的工具及展示摘要规则。
    /// </summary>
    /// <param name="workspacePath">代码工作区的完整路径。</param>
    /// <param name="assistantChatMessage">当前助手消息。</param>
    /// <returns>绑定本次运行的工具注册集合。</returns>
    IReadOnlyList<ToolRegistration> CreateRunToolRegistrations(
        string workspacePath,
        CopilotChatMessage assistantChatMessage) => [];
}
