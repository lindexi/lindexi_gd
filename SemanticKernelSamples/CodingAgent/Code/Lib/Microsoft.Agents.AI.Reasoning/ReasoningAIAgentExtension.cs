using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Microsoft.Agents.AI.Reasoning;

public static class ReasoningAIAgentExtension
{
    public static IAsyncEnumerable<ReasoningAgentResponseUpdate> RunReasoningStreamingAsync(this AIAgent agent, ChatMessage message,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RunReasoningStreamingAsync(agent, [message], session, options, cancellationToken);
    }

    public static async IAsyncEnumerable<ReasoningAgentResponseUpdate> RunReasoningStreamingAsync(this AIAgent agent, IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        bool? isThinking = null;
        bool hasOutputContent = false;
        bool isFirstOutputContent = true;

        await foreach (AgentResponseUpdate agentRunResponseUpdate in agent.RunStreamingAsync(messages, session, options, cancellationToken))
        {
            var type = agentRunResponseUpdate.GetType();
            Debug.WriteLine(type.FullName);

            var contentIsEmpty = string.IsNullOrEmpty(agentRunResponseUpdate.Text);
            var hasReasoningContent = false;

            foreach (var aiContent in agentRunResponseUpdate.Contents)
            {
                if (aiContent is FunctionCallContent functionCallContent)
                {
                    Debug.WriteLine($"FunctionCallContent {functionCallContent.Name}");
                }
                else if (aiContent is TextReasoningContent textReasoningContent)
                {
                    hasReasoningContent = true;

                    bool isFirstThinking = isThinking is null;
                    bool isReenterThinking = isThinking is false && hasOutputContent;

                    if (isThinking is null)
                    {
                        isThinking = true;
                    }

                    if (isThinking is false)
                    {
                        isThinking = true;
                    }

                    yield return new ReasoningAgentResponseUpdate(agentRunResponseUpdate)
                    {
                        Reasoning = textReasoningContent.Text,
                        IsFirstThinking = isFirstThinking,
                        IsReenterThinking = isReenterThinking,
                        IsFirstOutputContent = false,
                        IsReenterOutputContent = false,
                        IsThinkingEnd = false,
                    };
                }
                else if (aiContent is TextContent textContent)
                {
                    if (!string.IsNullOrEmpty(textContent.Text))
                    {
                        contentIsEmpty = false;
                    }
                }
            }

            if (contentIsEmpty && hasReasoningContent)
            {
                continue;
            }

            var responseUpdate = new ReasoningAgentResponseUpdate(agentRunResponseUpdate);

            if (!contentIsEmpty)
            {
                bool isEnteringOutputFromThinking = isThinking is true;

                if (isFirstOutputContent)
                {
                    responseUpdate.IsFirstOutputContent = true;
                }
                else if (isEnteringOutputFromThinking)
                {
                    responseUpdate.IsReenterOutputContent = true;
                }

                if (isEnteringOutputFromThinking)
                {
                    responseUpdate.IsThinkingEnd = true;
                }

                isFirstOutputContent = false;
                hasOutputContent = true;
                isThinking = false;

                yield return responseUpdate;
            }
            else
            {
                // 无文本内容，直接输出即可
                yield return responseUpdate;
            }
        }
    }
}