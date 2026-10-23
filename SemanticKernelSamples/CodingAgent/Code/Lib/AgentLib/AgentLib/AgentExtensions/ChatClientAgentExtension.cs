using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

namespace AgentLib.AgentExtensions;

/// <summary>
/// 提供 <see cref="AIAgent"/> 的扩展方法。
/// </summary>
public static class AIAgentExtension
{
    /// <summary>
    /// 以流式方式运行代理并将输出记录到控制台。
    /// </summary>
    /// <param name="agent">要运行的代理。</param>
    /// <param name="messages">输入消息集合。</param>
    /// <param name="session">可选的代理会话。</param>
    /// <param name="options">可选的运行选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public static async Task RunStreamingAndLogToConsoleAsync(this AIAgent agent, IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        UsageDetails totalUsage = new();

        await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(messages, session, options, cancellationToken))
        {
            if (reasoningAgentResponseUpdate.IsFirstThinking || reasoningAgentResponseUpdate.IsReenterThinking)
            {
                if (reasoningAgentResponseUpdate.IsReenterThinking)
                {
                    Console.WriteLine();
                    Console.WriteLine("-------------");
                }

                Console.WriteLine($"思考：");
            }

            if (reasoningAgentResponseUpdate.IsThinkingEnd
                && (reasoningAgentResponseUpdate.IsFirstOutputContent || reasoningAgentResponseUpdate.IsReenterOutputContent))
            {
                Console.WriteLine();
                Console.WriteLine("-------------");
            }

            Console.Write(reasoningAgentResponseUpdate.Reasoning);
            Console.Write(reasoningAgentResponseUpdate.Text);

            var usageContent = reasoningAgentResponseUpdate.Contents.OfType<UsageContent>().FirstOrDefault();
            if (usageContent?.Details is { } usageDetails)
            {
                totalUsage.InputTokenCount = SumNullable(totalUsage.InputTokenCount, usageDetails.InputTokenCount);
                totalUsage.OutputTokenCount = SumNullable(totalUsage.OutputTokenCount, usageDetails.OutputTokenCount);
                totalUsage.TotalTokenCount = SumNullable(totalUsage.TotalTokenCount, usageDetails.TotalTokenCount);
                totalUsage.CachedInputTokenCount = SumNullable(totalUsage.CachedInputTokenCount, usageDetails.CachedInputTokenCount);
                totalUsage.ReasoningTokenCount = SumNullable(totalUsage.ReasoningTokenCount, usageDetails.ReasoningTokenCount);
            }
        }

        if (HasUsage(totalUsage))
        {
            Console.WriteLine();
            Console.WriteLine("-------------");
            Console.WriteLine($"用量统计：输入 Token={totalUsage.InputTokenCount ?? 0}，输出 Token={totalUsage.OutputTokenCount ?? 0}，总 Token={totalUsage.TotalTokenCount ?? 0}");
        }
    }

    private static long? SumNullable(long? left, long? right)
    {
        return (left, right) switch
        {
            (null, null) => null,
            _ => (left ?? 0) + (right ?? 0)
        };
    }

    private static bool HasUsage(UsageDetails usageDetails)
    {
        return usageDetails.InputTokenCount.HasValue
               || usageDetails.OutputTokenCount.HasValue
               || usageDetails.TotalTokenCount.HasValue
               || usageDetails.CachedInputTokenCount.HasValue
               || usageDetails.ReasoningTokenCount.HasValue;
    }
}
