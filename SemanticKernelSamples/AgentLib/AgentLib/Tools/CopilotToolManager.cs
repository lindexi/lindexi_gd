using AgentLib.Core;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentLib.Tools;

/// <summary>
/// 管理 Copilot 可用的默认工具集合。
/// </summary>
public sealed class CopilotToolManager
{
    public CopilotToolManager(AgentApiEndpointManager agentApiEndpointManager)
    {
        ArgumentNullException.ThrowIfNull(agentApiEndpointManager);

        WorkspaceTools = new WorkspaceToolProvider();
        SubAgentTools = new SubAgentToolProvider(agentApiEndpointManager, WorkspaceTools);
    }

    public WorkspaceToolProvider WorkspaceTools { get; }

    public SubAgentToolProvider SubAgentTools { get; }

    public string? WorkspacePath
    {
        get => WorkspaceTools.WorkspacePath;
        set => WorkspaceTools.WorkspacePath = value;
    }

    public IReadOnlyList<AITool> CreateDefaultTools()
    {
        List<AITool> tools = [];
        tools.AddRange(WorkspaceTools.CreateDefaultTools());
        tools.AddRange(SubAgentTools.CreateDefaultTools());
        return tools;
    }
}
