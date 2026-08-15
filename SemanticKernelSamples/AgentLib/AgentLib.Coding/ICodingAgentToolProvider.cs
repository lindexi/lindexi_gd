using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed record CodingAgentToolSet(
    IReadOnlyList<AITool> Tools,
    ToolRegistrationRegistry ToolRegistrationRegistry);
