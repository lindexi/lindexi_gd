using Microsoft.Extensions.AI;

using System.Collections.Generic;
using System.Linq;

namespace AgentLib.Tools;

/// <summary>
/// 管理 Copilot 可用的默认工具集合。
/// </summary>
public sealed class CopilotToolManager
{
    public CopilotToolManager()
    {
        WorkspaceTools = new WorkspaceToolProvider();
    }

    public WorkspaceToolProvider WorkspaceTools { get; }

    public string? WorkspacePath
    {
        get => WorkspaceTools.WorkspacePath;
        set => WorkspaceTools.WorkspacePath = value;
    }

    public IReadOnlyList<AITool> CreateDefaultTools()
    {
        return WorkspaceTools.CreateDefaultTools().ToList();
    }
}
