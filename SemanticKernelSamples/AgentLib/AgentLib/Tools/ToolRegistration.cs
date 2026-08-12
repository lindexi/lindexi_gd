using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.Tools;

/// <summary>
/// 将可调用工具与其用户界面摘要规则绑定。
/// </summary>
public sealed record ToolRegistration(
    AITool Tool,
    Func<IDictionary<string, object?>, ToolCallPresentation>? CreatePresentation = null);
