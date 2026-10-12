namespace AgentLib.Model;

/// <summary>工具调用使用的展示片段类型。</summary>
public enum ToolCallDisplayKind
{
    /// <summary>普通工具调用。</summary>
    Tool,

    /// <summary>包含内部过程的子智能体调用。</summary>
    SubAgent,
}
