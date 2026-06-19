using AgentLib.Core;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AgentLib.Tools;

/// <summary>
/// 管理 Copilot 可用的默认工具集合。
/// </summary>
public sealed class CopilotToolManager
{
    /// <summary>
    /// 创建工具管理器。
    /// </summary>
    /// <param name="agentApiEndpointManager">API 终结点管理器。</param>
    public CopilotToolManager(AgentApiEndpointManager agentApiEndpointManager)
    {
        ArgumentNullException.ThrowIfNull(agentApiEndpointManager);

        WorkspaceTools = new WorkspaceToolProvider();
        SubAgentTools = new SubAgentToolProvider(agentApiEndpointManager, WorkspaceTools);
    }

    /// <summary>
    /// 工作区文件系统工具。
    /// </summary>
    public WorkspaceToolProvider WorkspaceTools { get; }

    /// <summary>
    /// 子智能体工具。
    /// </summary>
    public SubAgentToolProvider SubAgentTools { get; }

    /// <summary>
    /// 工作区路径。设置后将启用文件系统相关工具。
    /// </summary>
    public string? WorkspacePath
    {
        get => WorkspaceTools.WorkspacePath;
        set => WorkspaceTools.WorkspacePath = value;
    }

    internal string? PrimaryWorkspacePath => WorkspaceTools.PrimaryWorkspacePath;

    public string? SecondaryWorkspacePath
    {
        get => WorkspaceTools.SecondaryWorkspacePath;
        set => WorkspaceTools.SecondaryWorkspacePath = value;
    }

    internal IReadOnlyList<AITool> CreateDefaultTools(CopilotChatContext? chatContext, CancellationToken cancellationToken = default)
    {
        List<AITool> tools = [];
        tools.AddRange(WorkspaceTools.CreateDefaultTools());
        tools.AddRange(SubAgentTools.CreateDefaultTools(chatContext, cancellationToken));
        return tools;
    }
}
