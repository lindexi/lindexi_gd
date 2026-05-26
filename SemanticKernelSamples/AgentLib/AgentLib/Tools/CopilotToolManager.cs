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

    internal IReadOnlyList<AITool> CreateDefaultTools(CopilotChatContext? chatContext, CancellationToken cancellationToken = default)
    {
        List<AITool> tools = [];
        tools.AddRange(WorkspaceTools.CreateDefaultTools());
        tools.AddRange(SubAgentTools.CreateDefaultTools(chatContext, cancellationToken));
        return tools;
    }
}
