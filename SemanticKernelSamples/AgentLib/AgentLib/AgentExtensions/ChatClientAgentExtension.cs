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

public static class AIAgentExtension
{
    public static async Task RunStreamingAndLogToConsoleAsync(this AIAgent agent, IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(messages, session, options, cancellationToken))
        {
            if (reasoningAgentResponseUpdate.IsFirstThinking)
            {
                Console.WriteLine($"思考：");
            }

            if (reasoningAgentResponseUpdate.IsThinkingEnd && reasoningAgentResponseUpdate.IsFirstOutputContent)
            {
                Console.WriteLine();
                Console.WriteLine("-------------");
            }

            Console.Write(reasoningAgentResponseUpdate.Reasoning);
            Console.Write(reasoningAgentResponseUpdate.Text);
        }
    }
}
