using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Responses;

/// <summary>
/// 定义一次 Responses 编程任务的运行选项。
/// </summary>
public sealed record ResponsesCodingRunOptions
{
    /// <summary>
    /// 获取是否为本次运行提供 dotnet run 工具。
    /// </summary>
    public bool EnableDotNetRun { get; init; }

    /// <summary>
    /// 获取模型的推理强度。
    /// </summary>
    public ReasoningEffort? ReasoningEffort { get; init; }
}
